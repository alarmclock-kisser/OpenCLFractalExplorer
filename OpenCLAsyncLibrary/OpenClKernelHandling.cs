﻿using OpenTK.Compute.OpenCL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCLAsyncLibrary
{
	public class OpenClKernelHandling
	{
		private string Repopath;
		private ListBox LogList;
		private OpenClMemoryRegister MemR;
		private CLContext Context;
		private CLDevice Device;
		private CLPlatform Platform;
		private CLCommandQueue Queue;



		// ----- ----- ----- ATTRIBUTES ----- ----- ----- \\
		public CLKernel? Kernel = null;
		public string? KernelFile = null;

		public Panel? InputPanel = null;
		public long InputBufferPointer = 0;


		private Dictionary<CLKernel, string> kernelCache = [];



		// ----- ----- ----- LAMBDA ----- ----- ----- \\
		public Dictionary<string, string> Files => this.GetKernelFiles();

		public Dictionary<string, Type> Arguments => this.GetKernelArguments();




		// ----- ----- ----- CONSTRUCTORS ----- ----- ----- \\
		public OpenClKernelHandling(string repopath, OpenClMemoryRegister memorRegister, CLContext ctx, CLDevice dev, CLPlatform plat, CLCommandQueue que, ListBox? logList = null)
		{
			// Set attributes
			this.Repopath = repopath;
			this.MemR = memorRegister;
			this.Context = ctx;
			this.Device = dev;
			this.Platform = plat;
			this.Queue = que;
			this.LogList = logList ?? new ListBox();

			//this.PrecompileAllKernels(true);

		}




		// ----- ----- ----- METHODS ----- ----- ----- \\





		// ----- ----- ----- PUBLIC METHODS ----- ----- ----- \\
		// Log
		public void Log(string message = "", string inner = "", int indent = 0)
		{
			string msg = "[Kernel]: " + new string(' ', indent * 2) + message;

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
			// Dispose logic here
		}


		// Files
		public Dictionary<string, string> GetKernelFiles(string subdir = "Kernels")
		{
			string dir = Path.Combine(this.Repopath, subdir);

			// Build dir if it doesn't exist
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}

			// Get all .cl files in the directory
			string[] files = Directory.GetFiles(dir, "*.cl", SearchOption.AllDirectories);

			// Check if any files were found
			if (files.Length == 0)
			{
				this.Log("No kernel files found in directory: " + dir);
				return [];
			}

			// Verify each file
			Dictionary<string, string> verifiedFiles = [];
			foreach (string file in files)
			{
				string? verifiedFile = this.VerifyKernelFile(file);
				if (verifiedFile != null)
				{
					string? name = this.GetKernelName(verifiedFile);
					verifiedFiles.Add(verifiedFile, name ?? "N/A");
				}
			}

			// Return
			return verifiedFiles;
		}

		public string? VerifyKernelFile(string filePath)
		{
			// Check if file exists & is .cl
			if (!File.Exists(filePath))
			{
				this.Log("Kernel file not found: " + filePath);
				return null;
			}

			if (Path.GetExtension(filePath) != ".cl")
			{
				this.Log("Kernel file is not a .cl file: " + filePath);
				return null;
			}

			// Check if file is empty
			string[] lines = File.ReadAllLines(filePath);
			if (lines.Length == 0)
			{
				this.Log("Kernel file is empty: " + filePath);
				return null;
			}

			// Check if file contains kernel function
			if (!lines.Any(line => line.Contains("__kernel")))
			{
				this.Log("Kernel function not found in file: " + filePath);
				return null;
			}

			return Path.GetFullPath(filePath);
		}

		public string? GetKernelName(string filePath)
		{
			// Verify file
			string? verifiedFilePath = this.VerifyKernelFile(filePath);
			if (verifiedFilePath == null)
			{
				return null;
			}

			// Try to extract function name from kernel code text
			string code = File.ReadAllText(filePath);

			// Find index of first "__kernel void "
			int index = code.IndexOf("__kernel void ");
			if (index == -1)
			{
				this.Log("Kernel function not found in file: " + filePath);
				return null;
			}

			// Find index of first "(" after "__kernel void "
			int startIndex = index + "__kernel void ".Length;
			int endIndex = code.IndexOf("(", startIndex);
			if (endIndex == -1)
			{
				this.Log("Kernel function not found in file: " + filePath);
				return null;
			}

			// Extract function name
			string functionName = code.Substring(startIndex, endIndex - startIndex).Trim();
			if (functionName.Contains(" ") || functionName.Contains("\t") ||
				functionName.Contains("\n") || functionName.Contains("\r"))
			{
				this.Log("Kernel function name is invalid: " + functionName);
			}

			// Check if function name is empty
			if (string.IsNullOrEmpty(functionName))
			{
				this.Log("Kernel function name is empty: " + filePath);
				return null;
			}

			// Compare to file name without ext
			string fileName = Path.GetFileNameWithoutExtension(filePath);
			if (string.Compare(functionName, fileName, StringComparison.OrdinalIgnoreCase) != 0)
			{
				this.Log("Kernel function name does not match file name: " + filePath, "", 2);
			}

			return functionName;
		}


		// Compile
		public CLKernel? CompileFile(string filePath)
		{
			// Verify file
			string? verifiedFilePath = this.VerifyKernelFile(filePath);
			if (verifiedFilePath == null)
			{
				return null;
			}

			// Get kernel name
			string? kernelName = this.GetKernelName(verifiedFilePath);
			if (kernelName == null)
			{
				return null;
			}

			// Read kernel code
			string code = File.ReadAllText(verifiedFilePath);

			// Create program
			CLProgram program = CL.CreateProgramWithSource(this.Context, code, out CLResultCode error);
			if (error != CLResultCode.Success)
			{
				this.Log("Error creating program from source: " + error.ToString());
				return null;
			}

			// Create callback
			CL.ClEventCallback callback = new((program, userData) =>
			{
				// Check build log
				//
			});

			// When building the kernel
			string buildOptions = "-cl-std=CL1.2 -cl-fast-relaxed-math";
			CL.BuildProgram(program, 1, [this.Device], buildOptions, 0, IntPtr.Zero);

			// Build program
			error = CL.BuildProgram(program, [this.Device], buildOptions, callback);
			if (error != CLResultCode.Success)
			{
				this.Log("Error building program: " + error.ToString());

				// Get build log
				CLResultCode error2 = CL.GetProgramBuildInfo(program, this.Device, ProgramBuildInfo.Log, out byte[] buildLog);
				if (error2 != CLResultCode.Success)
				{
					this.Log("Error getting build log: " + error2.ToString());
				}
				else
				{
					string log = Encoding.UTF8.GetString(buildLog);
					this.Log("Build log: " + log, "", 1);
				}

				CL.ReleaseProgram(program);
				return null;
			}

			// Create kernel
			CLKernel kernel = CL.CreateKernel(program, kernelName, out error);
			if (error != CLResultCode.Success)
			{
				this.Log("Error creating kernel: " + error.ToString());

				// Get build log
				CLResultCode error2 = CL.GetProgramBuildInfo(program, this.Device, ProgramBuildInfo.Log, out byte[] buildLog);
				if (error2 != CLResultCode.Success)
				{
					this.Log("Error getting build log: " + error2.ToString());
				}
				else
				{
					string log = Encoding.UTF8.GetString(buildLog);
					this.Log("Build log: " + log, "", 1);
				}

				CL.ReleaseProgram(program);
				return null;
			}

			// Return kernel
			return kernel;
		}

		public Dictionary<string, Type> GetKernelArguments(CLKernel? kernel = null, string filePath = "")
		{
			Dictionary<string, Type> arguments = [];

			// Verify kernel
			kernel ??= this.Kernel;
			if (kernel == null)
			{
				// Try get kernel by file path
				kernel = this.CompileFile(filePath);
				if (kernel == null)
				{
					this.Log("Kernel is null");
					return arguments;
				}
			}

			// Get kernel info
			CLResultCode error = CL.GetKernelInfo(kernel.Value, KernelInfo.NumberOfArguments, out byte[] argCountBytes);
			if (error != CLResultCode.Success)
			{
				//this.Log("Error getting kernel info: " + error.ToString());
				return arguments;
			}

			// Get number of arguments
			int argCount = BitConverter.ToInt32(argCountBytes, 0);

			// Loop through arguments
			for (int i = 0; i < argCount; i++)
			{
				// Get argument info type name
				error = CL.GetKernelArgInfo(kernel.Value, (uint) i, KernelArgInfo.TypeName, out byte[] argTypeBytes);
				if (error != CLResultCode.Success)
				{
					//this.Log("Error getting kernel argument info: " + error.ToString());
					continue;
				}

				// Get argument info arg name
				error = CL.GetKernelArgInfo(kernel.Value, (uint) i, KernelArgInfo.Name, out byte[] argNameBytes);
				if (error != CLResultCode.Success)
				{
					//this.Log("Error getting kernel argument info: " + error.ToString());
					continue;
				}

				// Get argument type & name
				string argName = Encoding.UTF8.GetString(argNameBytes).TrimEnd('\0');
				string typeName = Encoding.UTF8.GetString(argTypeBytes).TrimEnd('\0');
				Type? type = null;

				// Switch for typeName
				if (typeName.EndsWith("*"))
				{
					typeName = typeName.Replace("*", "");
					switch (typeName)
					{
						case "int":
							type = typeof(int*);
							break;
						case "float":
							type = typeof(float*);
							break;
						case "long":
							type = typeof(long*);
							break;
						case "uchar":
							type = typeof(byte*);
							break;
						default:
							this.Log("Unknown pointer type: " + typeName, "", 2);
							break;
					}
				}
				else
				{
					switch (typeName)
					{
						case "int":
							type = typeof(int);
							break;
						case "float":
							type = typeof(float);
							break;
						case "double":
							type = typeof(double);
							break;
						case "char":
							type = typeof(char);
							break;
						case "uchar":
							type = typeof(byte);
							break;
						case "short":
							type = typeof(short);
							break;
						case "ushort":
							type = typeof(ushort);
							break;
						case "long":
							type = typeof(long);
							break;
						case "ulong":
							type = typeof(ulong);
							break;
						default:
							this.Log("Unknown argument type: " + typeName, "", 2);
							break;
					}
				}

				// Add to dictionary
				arguments.Add(argName, type ?? typeof(object));
			}

			// Return arguments
			return arguments;
		}

		public Dictionary<string, Type> GetKernelArgumentsAnalog(string? filepath)
		{
			Dictionary<string, Type> arguments = [];
			if (string.IsNullOrEmpty(filepath))
			{
				filepath = this.KernelFile;
			}

			// Read kernel code
			filepath = this.VerifyKernelFile(filepath ?? "");
			if (filepath == null)
			{
				this.Log("Kernel file not found or invalid: " + filepath);
				return arguments;
			}

			string code = File.ReadAllText(filepath);
			if (string.IsNullOrEmpty(code))
			{
				this.Log("Kernel code is empty: " + filepath);
				return arguments;
			}

			// Find kernel function
			int index = code.IndexOf("__kernel void ");
			if (index == -1)
			{
				this.Log("Kernel function not found in file: " + filepath);
				return arguments;
			}
			int startIndex = index + "__kernel void ".Length;
			int endIndex = code.IndexOf("(", startIndex);
			if (endIndex == -1)
			{
				this.Log("Kernel function not found in file: " + filepath);
				return arguments;
			}

			string functionName = code.Substring(startIndex, endIndex - startIndex).Trim();
			if (string.IsNullOrEmpty(functionName))
			{
				this.Log("Kernel function name is empty: " + filepath);
				return arguments;
			}

			if (functionName.Contains(" ") || functionName.Contains("\t") ||
				functionName.Contains("\n") || functionName.Contains("\r"))
			{
				this.Log("Kernel function name is invalid: " + functionName, "", 2);
			}

			// Get arguments string
			int argsStartIndex = code.IndexOf("(", endIndex) + 1;
			int argsEndIndex = code.IndexOf(")", argsStartIndex);
			if (argsEndIndex == -1)
			{
				this.Log("Kernel arguments not found in file: " + filepath);
				return arguments;
			}
			string argsString = code.Substring(argsStartIndex, argsEndIndex - argsStartIndex).Trim();
			if (string.IsNullOrEmpty(argsString))
			{
				this.Log("Kernel arguments are empty: " + filepath);
				return arguments;
			}

			string[] args = argsString.Split(',');

			foreach (string arg in args)
			{
				string[] parts = arg.Trim().Split(' ');
				if (parts.Length < 2)
				{
					this.Log("Kernel argument is invalid: " + arg, "", 2);
					continue;
				}
				string typeName = parts[^2].Trim();
				string argName = parts[^1].Trim().TrimEnd(';', ')', '\n', '\r', '\t');
				Type? type = null;
				if (typeName.EndsWith("*"))
				{
					typeName = typeName.Replace("*", "");
					switch (typeName)
					{
						case "int":
							type = typeof(int*);
							break;
						case "float":
							type = typeof(float*);
							break;
						case "long":
							type = typeof(long*);
							break;
						case "uchar":
							type = typeof(byte*);
							break;
						default:
							this.Log("Unknown pointer type: " + typeName, "", 2);
							break;
					}
				}
				else
				{
					switch (typeName)
					{
						case "int":
							type = typeof(int);
							break;
						case "float":
							type = typeof(float);
							break;
						case "double":
							type = typeof(double);
							break;
						case "char":
							type = typeof(char);
							break;
						case "uchar":
							type = typeof(byte);
							break;
						case "short":
							type = typeof(short);
							break;
						case "ushort":
							type = typeof(ushort);
							break;
						case "long":
							type = typeof(long);
							break;
						case "ulong":
							type = typeof(ulong);
							break;
						default:
							this.Log("Unknown argument type: " + typeName, "", 2);
							break;
					}
				}
				if (type != null)
				{
					arguments.Add(argName, type ?? typeof(object));
				}
			}

			return arguments;
		}

		public int GetArgumentPointerCount()
		{
			// Get kernel argument types
			Type[] argTypes = this.Arguments.Values.ToArray();

			// Count pointer arguments
			int count = 0;
			foreach (Type type in argTypes)
			{
				if (type.Name.EndsWith("*"))
				{
					count++;
				}
			}

			return count;
		}

		public object[] GetArgumentValues()
		{
			// Check panel set
			if (this.InputPanel == null || this.InputPanel.Controls.Count == 0)
			{
				this.Log("Input panel is null or empty");
				return [];
			}

			// Get kernel argument types
			Type[] argTypes = this.Arguments.Values.ToArray();

			// Get controls from panel with name containing "argInput"
			List<Control> controls = this.InputPanel.Controls.Cast<Control>()
				.Where(c => c.Name.Contains("argInput", StringComparison.OrdinalIgnoreCase))
				.ToList();

			// Get controls values
			List<object> values = [];

			int offset = 0;
			for (int i = 0; i < controls.Count; i++)
			{
				Control control = controls[i - offset];
				Type type = argTypes[i];

				// Get value from control
				if (control is NumericUpDown numericUpDown)
				{
					if (type == typeof(int))
					{
						values.Add((int) numericUpDown.Value);
					}
					if (type == typeof(float))
					{
						values.Add((float) numericUpDown.Value);
					}
				}
				else if (control is TextBox textBox)
				{
					values.Add(long.TryParse(textBox.Text, out long result) ? result : 0);
				}
				else if (control is Button button)
				{
					// Get color from button
					values.Add((int) button.BackColor.R);
					values.Add((int) button.BackColor.G);
					values.Add((int) button.BackColor.B);

					offset += 2;
				}
				else if (control is ComboBox comboBox)
				{
					values.Add(comboBox.SelectedItem ?? 0);
				}
				else if (control is CheckBox checkBox)
				{
					values.Add(checkBox.Checked);
				}
				else
				{
					this.Log("Unsupported control type: " + control.GetType().Name);
					values[i] = 0;
				}
			}

			return values.ToArray();
		}



		// UI
		public void FillGenericKernelVersionsCombobox(ComboBox comboBox, string baseName = "Kernel", bool caseSensitive = true)
		{
			// Clear combobox & text
			comboBox.Items.Clear();
			comboBox.Text = "Ver.";

			// Get all files witch contain baseName and a 2 char version in the name + caseSensitive
			string[] allFiles = Directory.GetFiles(Path.Combine(this.Repopath, "Kernels"), "*.cl", SearchOption.AllDirectories);

			// If caseSensitive is true, filter files
			string[] files = allFiles.Where(file => Path.GetFileNameWithoutExtension(file).Contains(baseName, caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
				.Where(file => Path.GetFileNameWithoutExtension(file).Length == baseName.Length + 2)
				.ToArray();

			// Get every files without extension last 2 chars
			foreach (string file in files)
			{
				string name = Path.GetFileNameWithoutExtension(file);
				comboBox.Items.Add(name.Substring(name.Length - 2, 2));
			}

			// Select last version
			if (comboBox.Items.Count > 0)
			{
				comboBox.SelectedIndex = comboBox.Items.Count - 1;
			}
		}

		public void FillGenericKernelNamesCombobox(ComboBox comboBox)
		{
			// Clear combobox
			comboBox.Items.Clear();

			string basePath = Path.Combine(this.Repopath, "Kernels");

			string[] files = Directory.GetFiles(basePath, "*.cl", SearchOption.AllDirectories);

			// Get every files without extension last 2 chars
			string[] baseNames = files
				.Select(file => Path.GetFileNameWithoutExtension(file))
				.Where(name => name.Length > 2)
				.Select(name => name.Substring(0, name.Length - 2))
				.Distinct()
				.ToArray();

			// Add base names to combobox
			foreach (string name in baseNames)
			{
				comboBox.Items.Add(name);
			}

			// Select -1
			comboBox.SelectedIndex = -1;
		}

		public TextBox? BuildInputPanel(Panel refPanel)
		{
			// Set input panel
			this.InputPanel = refPanel;

			// Clear input panel
			this.InputPanel.Controls.Clear();

			// Get kernel arguments
			Dictionary<string, Type> arguments = this.GetKernelArguments();

			// Check if arguments are empty
			if (arguments.Count == 0)
			{
				this.Log("No kernel arguments");
				return null;
			}

			// Loop through arguments
			int pointers = 0;
			int offset = 0;
			List<Label> colorLabels = [];
			List<NumericUpDown> colorInputs = [];
			List<Button> colorButtons = [];
			TextBox? inputBufferTextbox = null;
			for (int i = 0; i < arguments.Count; i++)
			{
				// Get argument name & type
				Type argType = arguments.ElementAt(i).Value;
				string prevName2 = i > 1 ? arguments.ElementAt(i - 2).Key : "";
				string prevName = i > 0 ? arguments.ElementAt(i - 1).Key : "";
				string argName = arguments.ElementAt(i).Key;
				string checkNextName = arguments.ElementAt(Math.Min(i + 1, arguments.Count - 1)).Key;
				string checkNextName2 = arguments.ElementAt(Math.Min(i + 2, arguments.Count - 1)).Key;
				string typeName = argType.Name;

				// Create label
				Label label = new();
				label.Name = "label_argName_" + argName;
				label.Text = argName;
				label.Location = new Point(10, offset);
				label.AutoSize = true;
				if (argName.ToLower().EndsWith("r") && checkNextName.ToLower().EndsWith("g") && checkNextName2.ToLower().EndsWith("b") ||
					argName.ToLower().EndsWith("g") && checkNextName.ToLower().EndsWith("b") && prevName.ToLower().EndsWith("r") ||
					argName.ToLower().EndsWith("b") && prevName2.ToLower().EndsWith("r") && prevName.ToLower().EndsWith("g"))
				{
					colorLabels.Add(label);
				}

				// Create input control based on type
				Control inputControl;
				if (argType == typeof(int))
				{
					inputControl = new NumericUpDown();
					((NumericUpDown) inputControl).Minimum = int.MinValue;
					((NumericUpDown) inputControl).Maximum = int.MaxValue;
					((NumericUpDown) inputControl).DecimalPlaces = 0;
					((NumericUpDown) inputControl).Increment = 1;
					((NumericUpDown) inputControl).Value = 0;
					// ((NumericUpDown) inputControl).ValueChanged += (s, e) => { this.Log("Value changed: " + ((NumericUpDown) s).Value); };

					// Pointer length if pointers is uneven
					if (pointers % 2 != 0)
					{
						inputControl.BackColor = Color.LightGray;
						inputControl.ForeColor = Color.Red;
						pointers++;
					}

					// Color input
					else if (argName.ToLower().EndsWith("r") && checkNextName.ToLower().EndsWith("g") && checkNextName2.ToLower().EndsWith("b") ||
						argName.ToLower().EndsWith("g") && checkNextName.ToLower().EndsWith("b") && prevName.ToLower().EndsWith("r") ||
						argName.ToLower().EndsWith("b") && prevName2.ToLower().EndsWith("r") && prevName.ToLower().EndsWith("g"))
					{
						inputControl.BackColor = Color.LightGray;
						colorInputs.Add((NumericUpDown) inputControl);
					}
				}
				else if (argType == typeof(float))
				{
					inputControl = new NumericUpDown();
					((NumericUpDown) inputControl).Minimum = decimal.MinValue;
					((NumericUpDown) inputControl).Maximum = decimal.MaxValue;
					((NumericUpDown) inputControl).DecimalPlaces = 5;
					((NumericUpDown) inputControl).Increment = 0.01M;
					((NumericUpDown) inputControl).Value = 0.5M;
					// ((NumericUpDown) inputControl).ValueChanged += (s, e) => { this.Log("Value changed: " + ((NumericUpDown) s).Value); };
				}
				else if (argType == typeof(double))
				{
					inputControl = new NumericUpDown();
					((NumericUpDown) inputControl).Minimum = decimal.MinValue;
					((NumericUpDown) inputControl).Maximum = decimal.MaxValue;
					((NumericUpDown) inputControl).DecimalPlaces = 10;
					((NumericUpDown) inputControl).Increment = 0.00001M;
					// ((NumericUpDown) inputControl).ValueChanged += (s, e) => { this.Log("Value changed: " + ((NumericUpDown) s).Value); };
				}
				else if (argType == typeof(string))
				{
					inputControl = new TextBox();
					// inputControl.TextChanged += (s, e) => { this.Log("Text changed: " + ((TextBox) s).Text); };
				}
				else if (argType == typeof(long))
				{
					inputControl = new NumericUpDown();
					((NumericUpDown) inputControl).Minimum = 0;
					((NumericUpDown) inputControl).Maximum = 99999999999;
					((NumericUpDown) inputControl).DecimalPlaces = 0;
					((NumericUpDown) inputControl).Increment = 1;
					// ((NumericUpDown) inputControl).ValueChanged += (s, e) => { this.Log("Value changed: " + ((NumericUpDown) s).Value); };
				}
				else if (argType == typeof(long*) || argType == typeof(float*) || argType == typeof(int*) || argType == typeof(uint*) || argType == typeof(ulong*) || argType == typeof(byte*))
				{
					inputControl = new TextBox();
					inputBufferTextbox = (TextBox) inputControl;
					// inputControl.TextChanged += (s, e) => { this.Log("Text changed: " + ((TextBox) s).Text); };
					inputControl.ForeColor = Color.Red;
					pointers++;
				}
				else
				{
					this.Log("Unsupported argument type or buffer argument found: " + argType.Name + " '" + argName + "'");
					inputControl = new TextBox();
					inputBufferTextbox = (TextBox) inputControl;
				}

				// Set input control properties
				inputControl.Location = new Point(10 + label.Width, offset);
				inputControl.Width = refPanel.Width - label.Width - 35;
				inputControl.Name = "argInput_" + argName;

				// DEBUG LOG name
				this.Log("Input control name: " + inputControl.Name, typeName, 2);

				// Add tooltip to input control
				ToolTip toolTip = new();
				toolTip.SetToolTip(inputControl, "'" + typeName + "'");

				// Add "Pointer" to input control name if type is pointer
				if (typeName.EndsWith("*"))
				{
					inputControl.Name = "argInputPointer_" + pointers + "_" + argName;
					inputControl.Text = this.InputBufferPointer.ToString();
					inputControl.BackColor = Color.LightGray;
					toolTip.ForeColor = Color.Red;
				}

				// Make invariable args also red
				string lowerName = argName.ToLower();
				if (lowerName.Contains("width") || lowerName.Contains("height") || lowerName.Contains("channels") || lowerName.Contains("bitdepth"))
				{
					inputControl.BackColor = Color.LightGray;
					inputControl.ForeColor = Color.DarkOrange;
				}

				// Add label and input control to panel
				this.InputPanel.Controls.Add(label);
				this.InputPanel.Controls.Add(inputControl);

				offset += 30;

				// Every 3: merge color inputs -> pick button
				if (colorInputs.Count == 3 && colorLabels.Count == 3)
				{
					// Remove color inputs & labels from panel
					for (int j = 0; j < colorLabels.Count; j++)
					{
						this.InputPanel.Controls.Remove(colorLabels[j]);
						this.InputPanel.Controls.Remove(colorInputs[j]);
						offset -= 30;
					}

					// Create label
					Label colorLabel = new();
					colorLabel.Name = "label_color_" + colorButtons.Count;
					colorLabel.Text = string.Join(", ", colorLabels.Select(l => "'" + l.Text + "'"));
					colorLabel.Location = new Point(10, offset);
					colorLabel.AutoSize = true;

					// Create button
					Button colorButton = new();
					colorButton.Name = "argInputColor_" + colorInputs.Count;
					colorButton.Text = "Pick color";
					colorButton.BackColor = Color.White;
					colorButton.Size = new Size(120, 23);

					// Location with padding
					colorButton.Location = new Point(this.InputPanel.Width - colorButton.Width - 25, offset);

					// Adjust label width
					if (colorLabel.Right > colorButton.Left - 5)
					{
						colorLabel.Width = colorButton.Left - 5 - colorLabel.Left;
						colorLabel.AutoEllipsis = true;
					}

					// Register event
					colorButton.Click += (s, e) =>
					{
						// Show color dialog
						ColorDialog colorDialog = new();
						if (colorDialog.ShowDialog() == DialogResult.OK)
						{
							// Set button color
							colorButton.BackColor = colorDialog.Color;

							// Adjust text color
							if (colorButton.BackColor.GetBrightness() < 0.5f)
							{
								colorButton.ForeColor = Color.White;
							}
						}
					};

					// Add button & label to list & panel
					colorButtons.Add(colorButton);
					this.InputPanel.Controls.Add(colorButton);
					this.InputPanel.Controls.Add(colorLabel);

					// Clear lists
					colorInputs.Clear();
					colorLabels.Clear();
				}
			}

			// If offset is more than panel height, make panel scrollable
			if (offset > this.InputPanel.Height)
			{
				this.InputPanel.AutoScroll = true;
				this.InputPanel.VerticalScroll.Visible = true;
			}
			else
			{
				this.InputPanel.AutoScroll = false;
				this.InputPanel.VerticalScroll.Visible = false;
			}

			// Return input buffer textbox
			return inputBufferTextbox;
		}



		// Load
		public void LoadKernel(string kernelName = "", string filePath = "", Panel? inputPanel = null)
		{
			// Verify panel
			inputPanel ??= this.InputPanel;

			// Get kernel file path
			if (!string.IsNullOrEmpty(filePath))
			{
				kernelName = Path.GetFileNameWithoutExtension(filePath);
			}
			else
			{
				filePath = Directory.GetFiles(Path.Combine(this.Repopath, "Kernels"), kernelName + "*.cl", SearchOption.AllDirectories).Where(f => Path.GetFileNameWithoutExtension(f).Length == kernelName.Length).FirstOrDefault() ?? "";
			}

			// Compile kernel if not cached
			if (this.Kernel != null && this.KernelFile == filePath)
			{
				this.Log("Kernel already loaded: " + kernelName, "", 1);
				return;
			}

			this.Kernel = this.CompileFile(filePath);
			this.KernelFile = filePath;



			// Check if kernel is null
			if (this.Kernel == null)
			{
				this.Log("Kernel is null");
				return;
			}
			else
			{
				// String of args like "(byte*)'pixels', (int)'width', (int)'height'"
				string argNamesString = string.Join(", ", this.Arguments.Keys.Select((arg, i) => $"({this.Arguments.Values.ElementAt(i).Name}) '{arg}'"));
				this.Log("Kernel loaded: '" + kernelName + "'", "", 1);
				this.Log("Kernel arguments: [" + argNamesString + "]", "", 1);
			}

			// Set file
			this.KernelFile = filePath;

			// TryAdd to cached
			this.kernelCache.TryAdd(this.Kernel.Value, filePath);

			// Build input panel
			//this.BuildInputPanel(inputPanel ?? new Panel());
		}

		public void UnloadKernel()
		{
			// Release kernel
			if (this.Kernel != null)
			{
				CL.ReleaseKernel(this.Kernel.Value);
				this.Kernel = null;
			}

			// Clear kernel file
			this.KernelFile = null;

			// Clear input panel
			if (this.InputPanel != null)
			{
				this.InputPanel.Controls.Clear();
				this.InputPanel = null;
			}
		}



		// EXEC
		public IntPtr ExecuteKernelGeneric(string version = "01", string baseName = "NULL", IntPtr pointer = 0, int width = 0, int height = 0, int channels = 4, int bitdepth = 8, object[] variableArguments = null, bool logSuccess = false)
		{
			// Start stopwatch
			List<long> times = [];
			List<string> timeNames = ["load: ", "mem: ", "args: ", "exec: ", "total: "];
			Stopwatch sw = Stopwatch.StartNew();

			// Get kernel path
			string kernelPath = this.Files.FirstOrDefault(f => f.Key.Contains(baseName + version)).Key ?? "";

			// Load kernel if not loaded
			if (this.Kernel == null || this.KernelFile != kernelPath)
			{
				this.LoadKernel(baseName + version);
				if (this.Kernel == null)
				{
					this.Log("Could not load Kernel '" + baseName + version + "'", $"ExecuteKernelIPGeneric({string.Join(", ", variableArguments ?? [])})");
					return pointer;
				}
			}

			// Take time
			times.Add(sw.ElapsedMilliseconds - times.Sum());

			// Get input buffer & length
			CLBuffer? inputBuffer = this.MemR.GetSingleBuffer(pointer, out IntPtr length);
			if (inputBuffer == null || length == IntPtr.Zero || length == 0)
			{
				this.Log("Input buffer not found or invalid length: " + pointer.ToString("X16"), length.ToString(), 2);
				return pointer;
			}

			// Get kernel arguments & work dimensions
			List<string> argNames = this.Arguments.Keys.ToList();

			// Dimensions
			int pixelsTotal = (int) length / 4; // Anzahl der Pixel
			int workWidth = width > 0 ? width : pixelsTotal; // Falls kein width gegeben, 1D
			int workHeight = height > 0 ? height : 1;        // Falls kein height, 1D

			// Work dimensions
			uint workDim = (width > 0 && height > 0) ? 2u : 1u;
			UIntPtr[] globalWorkSize = workDim == 2
				? [(UIntPtr) workWidth, (UIntPtr) workHeight]
				: [(UIntPtr) pixelsTotal];

			// Create output buffer
			IntPtr outputPointer = IntPtr.Zero;
			if (this.GetArgumentPointerCount() == 0)
			{
				this.Log("No I/O buffers found", "", 2);
				return pointer;
			}
			else if (this.GetArgumentPointerCount() == 1)
			{
				this.Log("In-place operation detected", "Single pointer kernel", 1);
			}
			else if (this.GetArgumentPointerCount() >= 2)
			{
				outputPointer = this.MemR.AllocateSingle<byte>(length);
				CLBuffer? outputBuffer = this.MemR.GetSingleBuffer(outputPointer, out nint outputBufferLength);
				if (outputBuffer == null || outputBufferLength != length)
				{
					this.Log("Output buffer not found or invalid length: " + outputPointer.ToString("X16"), outputBufferLength + " != " + length + " (?)", 2);
					return pointer;
				}
			}

			// Take time
			times.Add(sw.ElapsedMilliseconds - times.Sum());

			// Merge arguments
			List<object> arguments = this.MergeArguments(variableArguments, pointer, outputPointer, width, height, channels, bitdepth, false);

			// Set kernel arguments
			for (int i = 0; i < arguments.Count; i++)
			{
				// Set argument
				CLResultCode err = this.SetKernelArgSafe((uint) i, arguments[i]);
				if (err != CLResultCode.Success)
				{
					this.Log("Error setting kernel argument " + i + ": " + err.ToString(), arguments[i].ToString() ?? "");
					return pointer;
				}
			}

			// Take time
			times.Add(sw.ElapsedMilliseconds - times.Sum());

			// Log arguments
			if (logSuccess)
			{
				this.Log("Kernel arguments set: " + string.Join(", ", argNames.Select((a, i) => a + ": " + arguments[i].ToString())), "'" + baseName + version + "'", 2);
			}

			// Exec
			CLResultCode error = CL.EnqueueNDRangeKernel(
				this.Queue,
				this.Kernel.Value,
				workDim,          // 1D oder 2D
				null,             // Kein Offset
				globalWorkSize,   // Work-Größe in Pixeln
				null,             // Lokale Work-Size (automatisch)
				0, null, out CLEvent evt
			);
			if (error != CLResultCode.Success)
			{
				this.Log("Error executing kernel: " + error.ToString(), "", 2);
				return pointer;
			}

			// Wait for kernel to finish
			error = CL.WaitForEvents(1, [evt]);
			if (error != CLResultCode.Success)
			{
				this.Log("Error waiting for kernel to finish: " + error.ToString(), "", 2);
				return pointer;
			}

			// Release event
			error = CL.ReleaseEvent(evt);
			if (error != CLResultCode.Success)
			{
				this.Log("Error releasing event: " + error.ToString(), "", 2);
				return pointer;
			}

			// Take time
			times.Add(sw.ElapsedMilliseconds - times.Sum());
			times.Add(times.Sum());
			sw.Stop();

			// Free input buffer
			long freed;
			if (outputPointer == IntPtr.Zero)
			{
				freed = 0;
			}
			else
			{
				freed = this.MemR.FreeBuffer(pointer, true);
			}

			// Log success with timeNames
			if (logSuccess)
			{
				this.Log("Kernel executed successfully! Times: " + string.Join(", ", times.Select((t, i) => timeNames[i] + t + "ms")) + "(freed input: " + freed + "MB)", "'" + baseName + version + "'", 1);
			}

			// Return valued pointer
			return outputPointer != IntPtr.Zero ? outputPointer : pointer;
		}



		// Helpers
		public List<object> MergeArguments(object[] arguments, IntPtr inputPointer = 0, IntPtr outputPointer = 0, int width = 0, int height = 0, int channels = 4, int bitdepth = 8, bool log = false)
		{
			List<object> result = [];

			// Get kernel arguments
			Dictionary<string, Type> kernelArguments = this.GetKernelArguments(this.Kernel);
			if (kernelArguments.Count == 0)
			{
				this.Log("Kernel arguments not found", "", 2);
				kernelArguments = this.GetKernelArgumentsAnalog(this.KernelFile);
				if (kernelArguments.Count == 0)
				{
					this.Log("Kernel arguments not found", "", 2);
					return [];
				}
			}
			int bpp = bitdepth * channels;

			// Match arguments to kernel arguments
			bool inputFound = false;
			for (int i = 0; i < kernelArguments.Count; i++)
			{
				string argName = kernelArguments.ElementAt(i).Key;
				Type argType = kernelArguments.ElementAt(i).Value;

				// If argument is pointer -> add pointer
				if (argType.Name.EndsWith("*"))
				{
					// Get pointer value
					IntPtr argPointer = 0;
					if (!inputFound)
					{
						argPointer = (IntPtr) arguments[i] != 0 ? (IntPtr) arguments[i] : inputPointer;
						inputFound = true;
					}
					else
					{
						argPointer = (IntPtr) arguments[i] != 0 ? (IntPtr) arguments[i] : outputPointer;
					}

					// Get buffer
					CLBuffer? argBuffer = this.MemR.GetSingleBuffer(argPointer, out nint argLength);
					if (argBuffer == null || argLength == 0)
					{
						this.Log("Argument buffer not found or invalid length: " + argPointer.ToString("X16"), argLength.ToString(), 2);
						return [];
					}

					// Add pointer to result
					result.Add(argBuffer);

					// Log buffer found
					if (log)
					{
						// Log buffer found
						this.Log("Kernel argument buffer found: " + argPointer.ToString("X16"), "Index: " + i, 3);
					}
				}
				else if (argType == typeof(int))
				{
					// If name is "width" or "height" -> add width or height
					if (argName.ToLower() == "width")
					{
						result.Add(width <= 0 ? arguments[i] : width);

						// Log width found
						if (log)
						{
							this.Log("Kernel argument width found: " + width.ToString(), "Index: " + i, 3);
						}
					}
					else if (argName.ToLower() == "height")
					{
						result.Add(height <= 0 ? arguments[i] : height);

						// Log height found
						if (log)
						{
							this.Log("Kernel argument height found: " + height.ToString(), "Index: " + i, 3);
						}
					}
					else if (argName.ToLower() == "channels")
					{
						result.Add(channels <= 0 ? arguments[i] : channels);

						// Log channels found
						if (log)
						{
							this.Log("Kernel argument channels found: " + channels.ToString(), "Index: " + i, 3);
						}
					}
					else if (argName.ToLower() == "bitdepth")
					{
						result.Add(bitdepth <= 0 ? arguments[i] : bitdepth);

						// Log channels found
						if (log)
						{
							this.Log("Kernel argument bitdepth found: " + bitdepth.ToString(), "Index: " + i, 3);
						}
					}
					else if (argName.ToLower() == "bpp")
					{
						result.Add(bpp <= 0 ? arguments[i] : bpp);

						// Log channels found
						if (log)
						{
							this.Log("Kernel argument bpp found: " + bpp.ToString(), "Index: " + i, 3);
						}
					}
					else
					{
						result.Add((int) arguments[i]);
					}
				}
				else if (argType == typeof(float))
				{
					// Sicher konvertieren
					result.Add(Convert.ToSingle(arguments[i]));
				}
				else if (argType == typeof(double))
				{
					result.Add((double) arguments[i]);
				}
				else if (argType == typeof(long))
				{
					result.Add((long) arguments[i]);
				}
			}

			// Log arguments
			if (log)
			{
				this.Log("Kernel arguments: " + string.Join(", ", result.Select(a => a.ToString())), "'" + Path.GetFileName(this.KernelFile) + "'", 2);
			}

			return result;
		}

		public CLResultCode SetKernelArgSafe(uint index, object value)
		{
			// Check kernel
			if (this.Kernel == null)
			{
				this.Log("Kernel is null");
				return CLResultCode.InvalidKernelDefinition;
			}

			switch (value)
			{
				case CLBuffer buffer:
					return CL.SetKernelArg(this.Kernel.Value, index, buffer);

				case int i:
					return CL.SetKernelArg(this.Kernel.Value, index, i);

				case long l:
					return CL.SetKernelArg(this.Kernel.Value, index, l);

				case float f:
					return CL.SetKernelArg(this.Kernel.Value, index, f);

				case double d:
					return CL.SetKernelArg(this.Kernel.Value, index, d);

				case byte b:
					return CL.SetKernelArg(this.Kernel.Value, index, b);

				case IntPtr ptr:
					return CL.SetKernelArg(this.Kernel.Value, index, ptr);

				// Spezialfall für lokalen Speicher (Größe als uint)
				case uint u:
					return CL.SetKernelArg(this.Kernel.Value, index, new IntPtr(u));

				default:
					throw new ArgumentException($"Unsupported argument type: {value?.GetType().Name ?? "null"}");
			}
		}

		public Dictionary<CLKernel, string> PrecompileAllKernels(bool cache)
		{
			// Get all kernel files
			string[] kernelFiles = this.Files.Keys.ToArray();

			// Precompile all kernels
			Dictionary<CLKernel, string> precompiledKernels = [];
			foreach (string kernelFile in kernelFiles)
			{
				// Compile kernel
				CLKernel? kernel = this.CompileFile(kernelFile);
				if (kernel != null)
				{
					precompiledKernels.Add(kernel.Value, kernelFile);
				}
				else
				{
					this.Log("Error compiling kernel: " + kernelFile, "", 2);
				}
			}

			this.UnloadKernel();

			// Cache
			if (cache)
			{
				this.kernelCache = precompiledKernels;
			}

			return precompiledKernels;
		}

		public string GetLatestKernelFile(string searchName = "")
		{
			string[] files = this.Files.Keys.ToArray();

			// Get all files that contain searchName
			string[] filteredFiles = files.Where(file => file.Contains(searchName, StringComparison.OrdinalIgnoreCase)).ToArray();
			string latestFile = filteredFiles.Select(file => new FileInfo(file))
				.OrderByDescending(file => file.LastWriteTime)
				.FirstOrDefault()?.FullName ?? "";

			// Return latest file
			if (string.IsNullOrEmpty(latestFile))
			{
				this.Log("No kernel files found with name: " + searchName);
				return "";
			}

			return latestFile;
		}



		// ----- ----- ----- PROTECTED METHODS ----- ----- ----- \\





	}
}
