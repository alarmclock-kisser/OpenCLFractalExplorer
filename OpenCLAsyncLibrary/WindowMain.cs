using OpenCLAsyncLibrary;
using System.Diagnostics;

namespace OpenCLAsycLibrary
{
	public partial class WindowMain : Form
	{
		// ----- ----- ----- ATTRIBUTES ----- ----- ----- \\
		public string Repopath;

		public OpenClService CTXH;
		public GuiBuilder GUIB;
		public ImageRecorder REC;
		public ImageHandling IMGH;





		private Dictionary<NumericUpDown, int> previousNumericValues = [];
		private bool isProcessing;
		private Form? fullScreenForm;
		private bool isDragging;
		private Point mouseDownLocation;
		private Dictionary<string, object> currentOverlayArgs = [];
		private bool ctrlKeyPressed;
		private bool kernelExecutionRequired;
		private float mandelbrotZoomFactor = 1;
		private Stopwatch? stopwatch;

		// ----- ----- ----- CONSTRUCTORS ----- ----- ----- \\
		public WindowMain()
		{
			this.InitializeComponent();

			// Set repopath
			this.Repopath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\"));

			// Window position
			this.StartPosition = FormStartPosition.Manual;
			this.Location = new Point(0, 0);

			// Init CUDA
			this.CTXH = new OpenClService(this.Repopath, this.listBox_log, this.comboBox_devices);

			// Init GUIB
			this.GUIB = new GuiBuilder(this.Repopath, this.listBox_log, this.CTXH, this.panel_kernelArgs, this.checkBox_silent);

			// Init REC
			this.REC = new ImageRecorder(this.Repopath, this.label_cached);

			// Init IMGH
			this.IMGH = new ImageHandling(this.Repopath, this.listBox_images, this.pictureBox_view, null, this.label_meta);

			// REGISTER EVENTS
			this.comboBox_kernels.SelectedIndexChanged += (s, e) => this.LoadKernel(this.comboBox_kernels.SelectedItem?.ToString() ?? "");
			this.pictureBox_view.DoubleClick += (s, e) => this.fullScreen_DoubleClick(s, e);
			this.pictureBox_view.MouseDown += this.pictureBox_view_MouseDown;
			this.pictureBox_view.MouseMove += this.pictureBox_view_MouseMove;
			this.pictureBox_view.MouseUp += this.pictureBox_view_MouseUp;
			this.pictureBox_view.MouseWheel += this.pictureBox_view_MouseWheel;
			this.pictureBox_view.Paint += (s, e) => this.PictureBox_view_Paint(s, e);
			this.listBox_images.DoubleClick += (s, e) => this.MoveImage(this.listBox_images.SelectedIndex, true);
			this.listBox_log.DoubleClick += (s, e) => this.CopyLogToClipboard(this.listBox_log.SelectedIndex);

			// Toggle dark mode on
			this.checkBox_darkMode.Checked = true;

			// Fill devices
			this.CTXH.FillDevicesCombo(this.comboBox_devices);

			// Select intel device
			this.CTXH.SelectDeviceLike("Intel");

			// Fill kernels
			this.CTXH.KernelHandling?.FillGenericKernelNamesCombobox(this.comboBox_kernels);
			if (this.comboBox_kernels.Items.Contains("mandelbrotAutoPrecise"))
			{
				this.comboBox_kernels.SelectedItem = "mandelbrotAutoPrecise";
			}
			else if (this.comboBox_kernels.Items.Count > 0)
			{
				this.comboBox_kernels.SelectedIndex = 0;
			}

			// Add empty image
			this.button_create_Click(this, EventArgs.Empty);
		}








		// ----- ----- ----- METHODS ----- ----- ----- \\
		public void RenderArgsIntoPicturebox()
		{
			object[] argValues = this.GUIB.GetArgumentValues();
			string[] argNames = this.GUIB.GetArgumentNames();
			Dictionary<string, object> args = argValues
				.Select((value, index) => new { Name = argNames[index], Value = value })
				.ToDictionary(x => x.Name, x => x.Value);

			// Filter args (wie in deinem Code)
			args = args.Where(x => !x.Key.ToLower().Contains("input") &&
								  !x.Key.ToLower().Contains("output") &&
								  !x.Key.ToLower().Contains("pixel") &&
								  !x.Key.ToLower().Contains("width") &&
								  !x.Key.ToLower().Contains("height") &&
								  !x.Key.EndsWith("R") &&
								  !x.Key.EndsWith("G") &&
								  !x.Key.EndsWith("B"))
				.ToDictionary(x => x.Key, x => x.Value);

			// Optional: Add exec time
			if (this.label_execTime.Text != "Exec: -")
			{
				args.Add("Kernel", this.label_execTime.Text);
			}

			// Optional: Add recording and frame id
			if (this.REC.CachedImages.Count > 0)
			{
				args.Add("Recording", this.REC.CachedImages.Count);
			}

			// Optional: Add empty + zoom factor
			args.Add("", 0);
			args.Add("Zoom factor", this.numericUpDown_zoomFactor.Value);



			// Speichere args f�r Overlay-Zeichnung im Paint-Event
			this.currentOverlayArgs = args;

			// PictureBox neu zeichnen (ruft PictureBox_view_Paint auf)
			this.pictureBox_view.Invalidate();
		}

		public void MoveImage(int index = -1, bool refresh = true)
		{
			// Abort if CTRL down
			if (ModifierKeys == Keys.Control)
			{
				return;
			}

			if (index == -1 && this.IMGH.CurrentObject != null)
			{
				index = this.IMGH.Images.IndexOf(this.IMGH.CurrentObject);
			}

			if (index < 0 && index >= this.IMGH.Images.Count)
			{
				MessageBox.Show("Invalid index", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			ImageObject image = this.IMGH.Images[index];

			// Move image Host <-> CUDA
			if (image.OnHost)
			{
				// Move to CUDA: Get bytes
				byte[] bytes = image.GetPixelsAsBytes(false);

				// STOPWATCH
				Stopwatch sw = Stopwatch.StartNew();

				// Create buffer
				IntPtr pointer = this.CTXH.MemorRegister?.PushData(bytes) ?? 0;

				// STOPWATCH
				sw.Stop();
				//this.label_pushTime.Text = $"Push time: {sw.ElapsedMilliseconds} ms";

				// Check pointer
				if (pointer == 0)
				{
					MessageBox.Show("Failed to push data to CUDA", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}

				// Set pointer, void image
				image.Pointer = pointer;
				image.Img = new Bitmap(1, 1);
			}
			else if (image.OnDevice)
			{
				// STOPWATCH
				Stopwatch sw = Stopwatch.StartNew();

				// Move to Host
				byte[] bytes = this.CTXH.MemorRegister?.PullData<byte>(image.Pointer, false) ?? [];
				if (bytes.Length == 0)
				{
					MessageBox.Show("Failed to pull data from CUDA", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}

				// STOPWATCH
				sw.Stop();
				//this.label_pullTime.Text = $"Pull time: {sw.ElapsedMilliseconds} ms";

				// Create image
				image.SetImageFromBytes(bytes, true);
			}

			// Refill list
			if (refresh)
			{
				this.IMGH.FillImagesListBox();
			}
		}

		public void LoadKernel(string kernelName = "")
		{
			// Load kernel
			this.CTXH.KernelHandling?.LoadKernel(kernelName + this.comboBox_kernelVersions.SelectedItem?.ToString() ?? "01", "", this.panel_kernelArgs);
			if (this.CTXH.KernelHandling?.Kernel == null)
			{
				MessageBox.Show("Failed to load kernel", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			// Get arguments
			this.GUIB.BuildPanel(0.48f, !this.checkBox_invariables.Checked);

		}

		public void ExecuteKernelOOP(int index = -1, string kernelName = "")
		{
			// If index is -1, use current object
			if (index == -1 && this.IMGH.CurrentObject != null)
			{
				index = this.IMGH.Images.IndexOf(this.IMGH.CurrentObject);
			}

			if (index < 0 || index >= this.IMGH.Images.Count)
			{
				// No image selected: button_create_Click
				if (this.IMGH.CurrentObject == null)
				{
					this.button_create_Click(this, EventArgs.Empty);
					return;
				}
				if (this.IMGH.Images.Count == 0)
				{
					MessageBox.Show("No images available", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}
			}

			// Get image
			ImageObject? image = this.IMGH.Images[index];
			if (image == null)
			{
				MessageBox.Show("Invalid image", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			// Verify image on device
			bool moved = false;
			if (image.OnHost)
			{
				this.MoveImage(index, false);
				if (image.OnHost)
				{
					MessageBox.Show("Could not move image to device", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}

				moved = true;
			}

			// STOPWATCH
			Stopwatch sw = Stopwatch.StartNew();

			// Load kernel
			//this.CTXH.KernelHandling?.LoadKernel(kernelName + this.comboBox_kernelVersions.SelectedItem?.ToString() ?? "01", "", this.panel_kernelArgs);
			if (this.CTXH.KernelHandling?.Kernel == null)
			{
				MessageBox.Show("Failed to load kernel", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			// Get image attributes for kernel call
			IntPtr pointer = image.Pointer;
			int width = image.Width;
			int height = image.Height;
			int channels = 4;
			int bitdepth = image.BitsPerPixel / channels;

			// Get variable arguments
			object[] args = this.GUIB.GetArgumentValues();

			// Call exec kernel -> pointer
			if (this.CTXH.KernelHandling?.GetArgumentPointerCount() == 2)
			{
				image.Pointer = this.CTXH.KernelHandling?.ExecuteKernelGeneric(this.comboBox_kernelVersions.SelectedItem?.ToString() ?? "01", this.comboBox_kernels.SelectedItem?.ToString() ?? "", pointer, width, height, channels, bitdepth, args, !this.checkBox_silent.Checked) ?? image.Pointer;
			}
			else if (this.CTXH.KernelHandling?.GetArgumentPointerCount() == 1)
			{
				this.CTXH.KernelHandling?.ExecuteKernelGeneric(this.comboBox_kernelVersions.SelectedItem?.ToString() ?? "01", this.comboBox_kernels.SelectedItem?.ToString() ?? "", pointer, width, height, channels, bitdepth, args, !this.checkBox_silent.Checked);
			}
			else
			{
				MessageBox.Show("Invalid kernel arguments\n\nAbnormal amount of I/O pointers\n\nMust be 1 or 2", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

			// STOPWATCH
			sw.Stop();
			this.label_execTime.Text = $"Exec: {sw.ElapsedMilliseconds} ms";

			// Optional: Move back to host
			if (moved)
			{
				this.MoveImage(index, true);
			}

			// If kernel is Mandelbrot, cache image with interval
			if (image.Img != null && (this.checkBox_record.Checked || IsKeyLocked(Keys.CapsLock)))
			{
				this.REC.AddImage(image.Img, sw.ElapsedMilliseconds);
			}

			// Reset cache if checkbox is unchecked || cache isnt empty || not CAPS locked
			if (!this.checkBox_record.Checked && this.REC.CachedImages.Count != 0 && !IsKeyLocked(Keys.CapsLock))
			{
				this.REC.CachedImages.Clear();
				this.REC.CountLabel.Text = $"Images: -";
			}

			// Refill list
			this.IMGH.FitZoom();
		}

		private void CopyLogToClipboard(int selectedIndex)
		{
			// Check if index is valid
			if (selectedIndex < 0 || selectedIndex >= this.listBox_log.Items.Count)
			{
				MessageBox.Show("Invalid index", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			// Get selected item
			string selectedItem = this.listBox_log.Items[selectedIndex].ToString() ?? "";
			if (string.IsNullOrEmpty(selectedItem))
			{
				MessageBox.Show("No item selected", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			// Copy to clipboard
			Clipboard.SetText(selectedItem);

			MessageBox.Show("Copied to clipboard: " + selectedItem, "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}




		// ----- ----- ----- PRIVATES ----- ----- ----- \\
		private void pictureBox_view_MouseDown(object? sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				this.isDragging = true;
				this.mouseDownLocation = e.Location;
			}
		}

		private void pictureBox_view_MouseMove(object? sender, MouseEventArgs e)
		{
			if (!this.isDragging)
			{
				return;
			}

			try
			{
				// 1. Find NumericUpDown controls more efficiently
				NumericUpDown? numericX = this.panel_kernelArgs.Controls.OfType<NumericUpDown>().FirstOrDefault(x => x.Name.ToLower().Contains("offsetx"));
				NumericUpDown? numericY = this.panel_kernelArgs.Controls.OfType<NumericUpDown>().FirstOrDefault(x => x.Name.ToLower().Contains("offsety"));
				NumericUpDown? numericZ = this.panel_kernelArgs.Controls.OfType<NumericUpDown>().FirstOrDefault(x => x.Name.ToLower().Contains("zoom"));
				NumericUpDown? numericMouseX = this.panel_kernelArgs.Controls.OfType<NumericUpDown>().FirstOrDefault(x => x.Name.ToLower().Contains("mousex"));
				NumericUpDown? numericMouseY = this.panel_kernelArgs.Controls.OfType<NumericUpDown>().FirstOrDefault(x => x.Name.ToLower().Contains("mousey"));

				if (!(numericX == null || numericY == null || numericZ == null))
				{
					// 2. Calculate smooth delta with sensitivity factor and zoom
					float sensitivity = 0.001f * (float) (1 / numericZ.Value);
					decimal deltaX = (decimal) ((e.Location.X - this.mouseDownLocation.X) * -sensitivity);
					decimal deltaY = (decimal) ((e.Location.Y - this.mouseDownLocation.Y) * -sensitivity);

					// 3. Update values with boundary checks
					this.UpdateNumericValue(numericX, deltaX);
					this.UpdateNumericValue(numericY, deltaY);
				}

				// 4. Update mouse position for smoother continuous dragging
				this.mouseDownLocation = e.Location;

				// 5. Update mouse coordinates in NumericUpDown controls
				if (!(numericMouseX == null || numericMouseY == null))
				{
					this.UpdateNumericValue(numericMouseX, e.Location.X);
					this.UpdateNumericValue(numericMouseY, e.Location.Y);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"MouseMove error: {ex.Message}");
			}
		}

		private void pictureBox_view_MouseUp(object? sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				this.isDragging = false;

				// Re-execute kernel
				this.button_exec_Click(sender, e);

				this.RenderArgsIntoPicturebox();
			}
		}

		private void pictureBox_view_MouseWheel(object? sender, MouseEventArgs e)
		{
			// Check if SHIFT key is pressed
			if (Control.ModifierKeys == Keys.Shift)
			{
				// Change zoomLo value
				NumericUpDown? numericZoomLo = this.panel_kernelArgs.Controls.OfType<NumericUpDown>().FirstOrDefault(x => x.Name.ToLower().Contains("zoomlo"));
				if (numericZoomLo == null)
				{
					this.CTXH.KernelHandling?.Log("ZoomLo control not found!", "", 3);
					return;
				}

				// Increase/Decrease zoomLo
				if (e.Delta > 0)
				{
					numericZoomLo.Value += this.numericUpDown_zoomFactor.Value; // Increase by factor
				}
				else if (e.Delta < 0)
				{
					if (numericZoomLo.Value > numericZoomLo.Minimum)
					{
						numericZoomLo.Value -= this.numericUpDown_zoomFactor.Value; // Decrease by factor
					}
				}

				this.kernelExecutionRequired = false;
				this.button_exec_Click(sender, e);
				this.RenderArgsIntoPicturebox();



			}

			// Check for CTRL key press
			if (Control.ModifierKeys == Keys.Control)
			{
				this.ctrlKeyPressed = true;
				this.kernelExecutionRequired = true; // Set the flag
				NumericUpDown? numericI = this.panel_kernelArgs.Controls.OfType<NumericUpDown>().FirstOrDefault(x => x.Name.ToLower().Contains("iter"));
				if (numericI == null)
				{
					this.CTXH.KernelHandling?.Log("MaxIter control not found!", "", 3);
					return;
				}

				// Increase/Decrease maxIter
				if (e.Delta > 0)
				{
					numericI.Value += 2;
				}
				else if (e.Delta < 0)
				{
					if (numericI.Value > 0)
					{
						numericI.Value -= 2;
					}
				}

				return;
			}

			// Check if CTRL key was previously pressed
			if (this.ctrlKeyPressed)
			{
				this.ctrlKeyPressed = false; // Reset the flag
				this.kernelExecutionRequired = true;
			}

			// 1. Find NumericUpDown controls more efficiently
			NumericUpDown? numericZ = this.panel_kernelArgs.Controls.OfType<NumericUpDown>().FirstOrDefault(x => x.Name.ToLower().Contains("zoom"));
			if (numericZ == null)
			{
				// this.CTXH.KernelHandling?.Log("Zoom control not found!", "", 3);
				return;
			}

			// 2. Calculate zoom factor
			if (e.Delta > 0)
			{
				this.mandelbrotZoomFactor *= (float) this.numericUpDown_zoomFactor.Value;
			}
			else
			{
				this.mandelbrotZoomFactor /= (float) this.numericUpDown_zoomFactor.Value;
			}

			// 3. Update zoom value with boundary checks
			decimal newValue = (decimal) this.mandelbrotZoomFactor;
			if (newValue < numericZ.Minimum)
			{
				newValue = numericZ.Minimum;
			}
			if (newValue > numericZ.Maximum)
			{
				newValue = numericZ.Maximum;
			}
			numericZ.Value = newValue;

			// Call re-exec kernel
			this.kernelExecutionRequired = true;

			if (!this.ctrlKeyPressed && this.kernelExecutionRequired)
			{
				this.kernelExecutionRequired = false;
				this.button_exec_Click(sender, e);
				this.RenderArgsIntoPicturebox();
			}
		}

		private void PictureBox_view_Paint(object sender, PaintEventArgs e)
		{
			PictureBox? pbox = sender as PictureBox;
			if (pbox == null)
			{
				return;
			}

			// Erstens: Basis-Image zeichnen (falls noch nicht automatisch)
			if (pbox.Image != null)
			{
				e.Graphics.DrawImage(pbox.Image, new Point(0, 0));
			}

			// Overlay nur zeichnen, wenn MandelbrotMode aktiv ist und Overlay-Daten vorhanden sind
			if (this.currentOverlayArgs != null && this.currentOverlayArgs.Count > 0)
			{
				// Overlay erstellen - Gr��e auf PictureBox-Gr��e oder nach Wunsch
				Size overlaySize = new(pbox.Width / 8, pbox.Height / 8);

				// Overlay vom GuiBuilder holen (erwartet: CreateOverlayBitmap(Size, Dictionary, ...))
				Bitmap overlay = this.GUIB.CreateOverlayBitmap(overlaySize, this.currentOverlayArgs, fontSize: 12, color: Color.White, imageSize: pbox.Size);

				// Overlay transparent zeichnen an gew�nschter Position (z.B. oben links)
				e.Graphics.DrawImageUnscaled(overlay, new Point(10, 10));

				overlay.Dispose();
			}
		}

		private void PictureBox_view_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
			{
				this.fullScreenForm?.Close();
				this.fullScreenForm = null;
				return;
			}

			// KeyDown [c]: Toggle crosshair
			if (e.KeyCode == Keys.C)
			{
				this.checkBox_crosshair.Checked = !this.checkBox_crosshair.Checked;
				this.checkBox_crosshair_CheckedChanged(sender, e);
				return;
			}
		}


		private void RegisterNumericToSecondPow(NumericUpDown numeric)
		{
			// Initialwert speichern
			this.previousNumericValues.Add(numeric, (int) numeric.Value);

			numeric.ValueChanged += (s, e) =>
			{
				// Verhindere rekursive Aufrufe
				if (this.isProcessing)
				{
					return;
				}

				this.isProcessing = true;

				try
				{
					int newValue = (int) numeric.Value;
					int oldValue = this.previousNumericValues[numeric];
					int max = (int) numeric.Maximum;
					int min = (int) numeric.Minimum;

					// Nur verarbeiten, wenn sich der Wert tats chlich ge ndert hat
					if (newValue != oldValue)
					{
						int calculatedValue;

						if (newValue > oldValue)
						{
							// Verdoppeln, aber nicht  ber Maximum
							calculatedValue = Math.Min(oldValue * 2, max);
						}
						else if (newValue < oldValue)
						{
							// Halbieren, aber nicht unter Minimum
							calculatedValue = Math.Max(oldValue / 2, min);
						}
						else
						{
							calculatedValue = oldValue;
						}

						// Nur aktualisieren wenn notwendig
						if (calculatedValue != newValue)
						{
							numeric.Value = calculatedValue;
						}

						this.previousNumericValues[numeric] = calculatedValue;
					}
				}
				finally
				{
					this.isProcessing = false;
				}
			};
		}

		private void UpdateNumericValue(NumericUpDown numeric, decimal delta)
		{
			decimal newValue = numeric.Value + delta;

			// Ensure value stays within allowed range
			if (newValue < numeric.Minimum)
			{
				newValue = numeric.Minimum;
			}

			if (newValue > numeric.Maximum)
			{
				newValue = numeric.Maximum;
			}

			numeric.Value = newValue;
		}





		// ----- ----- ----- OVERRIDES ----- ----- ----- \\
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			if (WindowState == FormWindowState.Maximized)
			{
				// Beispiel: Auf 1280x800 begrenzen
				MaximumSize = new Size(1280, 800);
				WindowState = FormWindowState.Normal; // Zur�ckschalten, um Resize zuzulassen
				Size = MaximumSize;
			}
		}





		// ----- ----- ----- EVENT HANDLERS ----- ----- ----- \\
		private void checkBox_darkMode_CheckedChanged(object sender, EventArgs e)
		{
			DarkModeToggle.ToggleDarkMode(this, this.checkBox_darkMode.Checked);
		}

		private void fullScreen_DoubleClick(object? sender, EventArgs e)
		{
			// Abort if no kernel loaded
			if (this.CTXH.KernelHandling?.Kernel == null)
			{
				MessageBox.Show("No kernel loaded", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				this.fullScreenForm?.Close();
				this.fullScreenForm = null;
				return;
			}

			if (this.fullScreenForm != null)
			{
				// Bereits aktiv, schlie�en
				this.fullScreenForm.Close();
				this.fullScreenForm = null;
				return;
			}

			// Neues Fullscreen-Form erstellen
			this.fullScreenForm = new Form
			{
				FormBorderStyle = FormBorderStyle.None,
				TopMost = true,
				WindowState = FormWindowState.Maximized,
				BackColor = Color.Black,
				StartPosition = FormStartPosition.Manual,
				KeyPreview = true
			};

			// Pfeiltasten-Event f�r Fullscreen-Form
			this.fullScreenForm.KeyDown += (s, args) =>
			{
				if (args.KeyCode == Keys.Escape)
				{
					this.fullScreenForm?.Close();
					this.fullScreenForm = null;
					return;
				}

				if (args.KeyCode == Keys.C)
				{
					this.checkBox_crosshair.Checked = !this.checkBox_crosshair.Checked;
					this.button_exec_Click(sender, e);
				}

				// Beispiel: Reagiere auf Pfeiltasten
				if (args.KeyCode == Keys.Left)
				{
					NumericUpDown? numericRotY = this.panel_kernelArgs.Controls.OfType<NumericUpDown>().FirstOrDefault(x => x.Name.ToLower().Contains("rot") && x.Name.ToLower().Contains("y"));
					if (numericRotY != null)
					{
						this.UpdateNumericValue(numericRotY, 0.033M);
						this.button_exec_Click(sender, e);
					}
					return;
				}
				else if (args.KeyCode == Keys.Right)
				{
					NumericUpDown? numericRotY = this.panel_kernelArgs.Controls.OfType<NumericUpDown>().FirstOrDefault(x => x.Name.ToLower().Contains("rot") && x.Name.ToLower().Contains("y"));
					if (numericRotY != null)
					{
						this.UpdateNumericValue(numericRotY, -0.033M);
						this.button_exec_Click(sender, e);
					}
					return;
				}
				else if (args.KeyCode == Keys.Up)
				{
					NumericUpDown? numericRotX = this.panel_kernelArgs.Controls.OfType<NumericUpDown>().FirstOrDefault(x => x.Name.ToLower().Contains("rot") && x.Name.ToLower().Contains("x"));
					if (numericRotX != null)
					{
						this.UpdateNumericValue(numericRotX, 0.033M);
						this.button_exec_Click(sender, e);
					}
					return;
					
				}
				else if (args.KeyCode == Keys.Down)
				{
					NumericUpDown? numericRotX = this.panel_kernelArgs.Controls.OfType<NumericUpDown>().FirstOrDefault(x => x.Name.ToLower().Contains("rot") && x.Name.ToLower().Contains("x"));
					if (numericRotX != null)
					{
						this.UpdateNumericValue(numericRotX, -0.033M);
						this.button_exec_Click(sender, e);
					}
					return;
				}
				else if (args.KeyCode == Keys.PageUp)
				{
					NumericUpDown? numericRotZ = this.panel_kernelArgs.Controls.OfType<NumericUpDown>().FirstOrDefault(x => x.Name.ToLower().Contains("rot") && x.Name.ToLower().Contains("z"));
					if (numericRotZ != null)
					{
						this.UpdateNumericValue(numericRotZ, -0.033M);
						this.button_exec_Click(sender, e);
					}
					return;
				}
				else if (args.KeyCode == Keys.PageDown)
				{
					NumericUpDown? numericRotZ = this.panel_kernelArgs.Controls.OfType<NumericUpDown>().FirstOrDefault(x => x.Name.ToLower().Contains("rot") && x.Name.ToLower().Contains("z"));
					if (numericRotZ != null)
					{
						this.UpdateNumericValue(numericRotZ, 0.033M);
						this.button_exec_Click(sender, e);
					}
					return;
				}
			};


			// PictureBox in Originalgr��e oder gestreckt anzeigen
			PictureBox pb = new()
			{
				SizeMode = PictureBoxSizeMode.AutoSize,
				Image = this.pictureBox_view.Image,
				BackColor = Color.Black
			};

			// Reuse all existing event handlers
			pb.MouseDown += this.pictureBox_view_MouseDown!;
			pb.MouseMove += this.pictureBox_view_MouseMove!;
			pb.MouseUp += this.pictureBox_view_MouseUp!;
			pb.MouseWheel += this.pictureBox_view_MouseWheel!;
			pb.Paint += this.PictureBox_view_Paint!;
			pb.KeyDown += this.PictureBox_view_KeyDown!;
			pb.Focus(); // Damit MouseWheel funktioniert



			this.fullScreenForm.Controls.Add(pb);
			this.IMGH.SetPictureBox(pb);

			// ESC beenden
			this.fullScreenForm.KeyDown += (s, args) =>
			{
				if (args.KeyCode == Keys.Escape)
				{
					this.fullScreenForm?.Close();
					this.fullScreenForm = null;

					//this.IMGH.SetPictureBox(this.pictureBox_view);
				}
			};


			this.fullScreenForm.Show();
			this.fullScreenForm.Focus(); // wichtig f�r ESC
		}

		private void checkBox_crosshair_CheckedChanged(object sender, EventArgs e)
		{
			// Toggle crosshair in picturebox
			if (this.checkBox_crosshair.Checked)
			{
				this.IMGH.ShowCrosshair = true;
			}
			else
			{
				this.IMGH.ShowCrosshair = false;
			}

			this.pictureBox_view.Invalidate();
		}

		private void button_exec_Click(object? sender, EventArgs e)
		{
			string kernelName = this.comboBox_kernels.SelectedItem?.ToString() ?? "";

			this.ExecuteKernelOOP(-1, kernelName);
		}

		private void button_create_Click(object sender, EventArgs e)
		{
			this.IMGH.CreateEmpty(Color.Black, new Size(Screen.PrimaryScreen?.Bounds.Width ?? 0, Screen.PrimaryScreen?.Bounds.Height ?? 0), "Fullscreen_");
		}

		private void button_import_Click(object sender, EventArgs e)
		{
			this.IMGH.ImportImage();
		}

		private async void button_export_Click(object sender, EventArgs e)
		{
			if (this.REC.CachedImages.Count == 0)
			{
				if (this.IMGH.CurrentObject == null)
				{
					MessageBox.Show("No image to export", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}

				this.IMGH.CurrentObject.Export();
				return;
			}

			// Get special path MyPictures
			string myPicturesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "CUDA-GIFs");


			string result = await this.REC.CreateGifAsync(myPicturesPath, this.IMGH.CurrentObject?.Name ?? "animated_", (int) this.numericUpDown_fps.Value, true, null, this.progressBar_loading);

			// If CAPS locked, unlock Caps
			if (IsKeyLocked(Keys.CapsLock))
			{

			}

			// Remove image
			this.IMGH.RemoveImage();
			this.button_create_Click(sender, e);

			MessageBox.Show("Exported GIF to: " + result, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void comboBox_kernels_SelectedIndexChanged(object sender, EventArgs e)
		{
			this.CTXH.KernelHandling?.FillGenericKernelVersionsCombobox(this.comboBox_kernelVersions, this.comboBox_kernels.SelectedItem?.ToString() ?? "");
		}
	}
}
