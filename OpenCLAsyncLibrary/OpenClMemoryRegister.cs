using OpenTK.Compute.OpenCL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCLAsyncLibrary
{
	public class OpenClMemoryRegister
	{
		private string Repopath;
		private ListBox LogList;
		private CLContext Context;
		private CLDevice Device;
		private CLPlatform Platform;





		// ----- ----- ----- ATTRIBUTES ----- ----- ----- \\
		public CLCommandQueue QUE;


		public Dictionary<CLBuffer, IntPtr> SingleBuffers = [];
		public Dictionary<CLBuffer[], IntPtr[]> ArrayBuffers = [];



		// ----- ----- ----- CONSTRUCTORS ----- ----- ----- \\
		public OpenClMemoryRegister(string repopath, CLContext context, CLDevice device, CLPlatform platform, ListBox? logList = null)
		{
			this.Repopath = repopath;
			this.Context = context;
			this.Device = device;
			this.Platform = platform;
			this.LogList = logList ?? new ListBox();

			// Init. queue
			this.QUE = CL.CreateCommandQueueWithProperties(this.Context, this.Device, 0, out CLResultCode error);
			if (error != CLResultCode.Success)
			{
				this.Log("Failed to create CL-CommandQueue.");
			}

			
		}




		// ----- ----- ----- METHODS ----- ----- ----- \\
		public void Log(string message = "", string inner = "", int indent = 0)
		{
			string msg = "[Memory]: " + new string(' ', indent * 2) + message;

			if (!string.IsNullOrEmpty(inner))
			{
				msg += " (" + inner + ")";
			}

			// Add to logList
			this.LogList.Items.Add(msg);

			// Scroll down
			this.LogList.SelectedIndex = this.LogList.Items.Count - 1;
		}

		// Dispose
		public void Dispose()
		{
			// Dispose every single buffer
			foreach (CLBuffer buf in this.SingleBuffers.Keys)
			{
				this.FreeBuffer(buf.Handle);
			}

			// Dispose every array buffer
			foreach (nint kvp in this.ArrayBuffers.Keys.Select(k => k.FirstOrDefault().Handle))
			{
				this.FreeBuffer(kvp);
			}

		}


		// Free buffer
		public long FreeBuffer(IntPtr pointer, bool readable = false)
		{
			long freedSizeBytes = 0;

			// Get single buffer, if null, get array buffers
			CLBuffer? buffer = this.GetSingleBuffer(pointer, out IntPtr length);
			if (buffer != null)
			{
				// Get size
				freedSizeBytes = length.ToInt64();
				if (readable)
				{
					freedSizeBytes /= (1024 * 1024);
				}

				// Free single buffer
				CLResultCode err = CL.ReleaseMemoryObject(buffer.Value);
				if (err != CLResultCode.Success)
				{
					this.Log("Failed to free buffer", err.ToString());
				}
				this.SingleBuffers.Remove(buffer.Value);
				return freedSizeBytes;
			}
			else
			{
				// Get array buffers
				CLBuffer[] buffers = this.GetArrayBuffers(pointer, out IntPtr[] lengths);
				if (buffers.Length > 0)
				{
					// Get size
					freedSizeBytes = lengths.Sum(l => l.ToInt64());
					if (readable)
					{
						freedSizeBytes /= (1024 * 1024);
					}

					// Free array buffers
					foreach (CLBuffer buf in buffers)
					{
						CLResultCode err = CL.ReleaseMemoryObject(buf);
					}
					this.ArrayBuffers.Remove(buffers);
					return freedSizeBytes;
				}
			}

			// If no buffer found, return 0
			this.Log("No buffer found to free", pointer.ToString("X16"));
			return 0;
		}


		// Buffer info
		public Type GetBufferType(IntPtr pointer)
		{
			// Get single buffer, if null, get array buffers
			CLBuffer? buffer = this.GetSingleBuffer(pointer, out IntPtr length);
			if (buffer != null)
			{
				// Get type
				CLResultCode err = CL.GetMemObjectInfo(buffer.Value, MemoryObjectInfo.Type, out byte[] typeBytes);
				if (err != CLResultCode.Success)
				{
					this.Log("Failed to get buffer type", err.ToString());
					return typeof(void);
				}

				// Convert to string
				string typeStr = Encoding.UTF8.GetString(typeBytes).Trim('\0');
				Type type = Type.GetType(typeStr) ?? typeof(void);
				if (type == typeof(void))
				{
					this.Log("Failed to convert buffer type", typeStr);
				}

				// Return type
				return type;
			}
			else
			{
				// Get array buffers
				CLBuffer[] buffers = this.GetArrayBuffers(pointer, out IntPtr[] lengths);
				if (buffers.Length > 0)
				{
					// Get first buffer's type
					return this.GetBufferType(buffers[0].Handle);
				}

			}

			// If no buffer found, return void
			this.Log("No buffer found to get type", pointer.ToString("X16"));
			return typeof(void);
		}


		// Single buffer
		public CLBuffer? GetSingleBuffer(IntPtr pointer, out IntPtr length)
		{
			// Init. length
			length = new IntPtr(0);

			// Try to find handle in SingleBuffers 
			CLBuffer? buffer = null;
			foreach (CLBuffer buf in this.SingleBuffers.Keys)
			{
				if (buf.Handle == pointer)
				{
					buffer = buf;
					length = this.SingleBuffers[buffer.Value];
				}
			}

			// Return buffer
			return buffer;
		}

		public IntPtr PushData<T>(T[] data) where T : unmanaged
		{
			// Check data
			if (data.LongLength < 1)
			{
				return 0;
			}

			// Get IntPtr length
			IntPtr length = new(data.LongLength);

			// Create buffer
			CLBuffer buffer = CL.CreateBuffer<T>(this.Context, MemoryFlags.CopyHostPtr | MemoryFlags.ReadWrite, data, out CLResultCode error);
			if (error != CLResultCode.Success)
			{
				this.Log("Error creating CL-Buffer", error.ToString());
				return 0;
			}

			// Add to dict
			this.SingleBuffers.Add(buffer, length);

			// Get handle
			IntPtr handle = buffer.Handle;

			// Return handle
			return handle;
		}

		public T[] PullData<T>(IntPtr pointer, bool keep = false) where T : unmanaged
		{
			// Get buffer & length
			CLBuffer? buffer = this.GetSingleBuffer(pointer, out IntPtr length);
			if (buffer == null || length == 0)
			{
				return [];
			}

			// New array with length
			T[] data = new T[length];

			// Read buffer
			CLResultCode error = CL.EnqueueReadBuffer(
				this.QUE,
				buffer.Value,
				true,
				0,
				data,
				null,
				out CLEvent @event
			);

			// Check error
			if (error != CLResultCode.Success)
			{
				this.Log("Failed to read buffer", error.ToString(), 1);
				return [];
			}

			// If not keeping, free buffer
			if (!keep)
			{
				this.FreeBuffer(pointer);
			}

			// Return data
			return data;
		}

		public IntPtr AllocateSingle<T>(IntPtr size) where T : unmanaged
		{
			// Check size
			if (size.ToInt64() < 1)
			{
				return 0;
			}

			// Create empty array of type and size
			T[] data = new T[size.ToInt64()];
			data = data.Select(x => default(T)).ToArray();

			// Create buffer
			CLBuffer buffer = CL.CreateBuffer<T>(this.Context, MemoryFlags.CopyHostPtr | MemoryFlags.ReadWrite, data, out CLResultCode error);
			if (error != CLResultCode.Success)
			{
				this.Log("Error creating CL-Buffer", error.ToString());
				return 0;
			}

			// Add to dict
			this.SingleBuffers.Add(buffer, size);

			// Get handle
			IntPtr handle = buffer.Handle;

			// Return handle
			return handle;
		}


		// Array buffer
		public CLBuffer[] GetArrayBuffers(long indexPointer, out IntPtr[] lengths)
		{
			// Init. lengths
			lengths = [];

			// Try to find buffer array by first buffer's handle in ArrayBuffers dict
			foreach (KeyValuePair<CLBuffer[], nint[]> kvp in this.ArrayBuffers)
			{
				if (kvp.Key[0].Handle == indexPointer)
				{
					lengths = kvp.Value;
					return kvp.Key;
				}
			}

			// Return empty array if not found
			return [];
		}

		public long PushChunks<T>(List<T[]> chunks) where T : unmanaged
		{
			// Check chunks
			if (chunks.Count < 1 || chunks.Any(chunk => chunk.LongLength < 1))
			{
				return 0;
			}

			// Get IntPtr[] lengths
			IntPtr[] lengths = chunks.Select(chunk => new IntPtr(chunk.LongLength)).ToArray();

			// Create buffers for each chunk
			CLBuffer[] buffers = new CLBuffer[chunks.Count];
			for (int i = 0; i < chunks.Count; i++)
			{
				buffers[i] = CL.CreateBuffer(this.Context, MemoryFlags.AllocHostPtr | MemoryFlags.ReadWrite, chunks[i], out CLResultCode error);
				if (error != CLResultCode.Success)
				{
					this.Log("Error creating CL-Buffer for chunk " + i);
					return 0;
				}
			}

			// Add to dict with lengths
			this.ArrayBuffers.Add(buffers, lengths);

			// Get first buffer's handle
			long handle = buffers[0].Handle;

			// Return handle
			return handle;
		}

		public List<T[]> PullChunks<T>(long indexPointer) where T : unmanaged
		{
			// Get buffers & lengths
			CLBuffer[] buffers = this.GetArrayBuffers(indexPointer, out IntPtr[] lengths);
			if (buffers.Length == 0 || lengths.Length == 0)
			{
				return [];
			}

			// Chunk list
			List<T[]> chunks = [];

			// Read every buffer
			for (int i = 0; i < buffers.Length; i++)
			{
				T[] chunk = new T[lengths[i].ToInt64()];
				CLResultCode error = CL.EnqueueReadBuffer(
					this.QUE,
					buffers[i],
					true,
					0,
					chunk,
					null,
					out CLEvent @event
				);

				if (error != CLResultCode.Success)
				{
					this.Log("Failed to read buffer for chunk " + i, error.ToString(), 1);
					return [];
				}

				chunks.Add(chunk);
			}

			// Return chunks
			return chunks;
		}


		// UI
		public void FillPointersListbox(ListBox listBox)
		{
			// Clear listbox
			listBox.Items.Clear();

			// Add (1) Single Buffers
			foreach (CLBuffer buf in this.SingleBuffers.Keys)
			{
				listBox.Items.Add("(1) " + buf.Handle.ToString("X16"));
			}

			// Add (n) Array Buffers
			foreach (KeyValuePair<CLBuffer[], nint[]> kvp in this.ArrayBuffers)
			{
				listBox.Items.Add("(" + kvp.Key.Length + ") " + kvp.Key.FirstOrDefault().Handle.ToString("X16"));
			}
		}



		// ----- ----- ----- PROTECTED METHODS ----- ----- ----- \\





	}
}
