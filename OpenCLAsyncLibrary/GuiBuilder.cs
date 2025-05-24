﻿
using OpenCLAsyncLibrary;
using System.Drawing.Drawing2D;

namespace OpenCLAsycLibrary
{
	public class GuiBuilder
	{
		// ----- ----- ATTRIBUTES ----- ----- \\
		private string Repopath;
		private ListBox LogList;
		private OpenClService ContextH;
		private Panel ArgumentsPanel;
		private CheckBox SilenceCheck;



		private List<NumericUpDown> NumericsList = [];
		private List<Label> LabelList = [];
		private string CuPath => Path.Combine(this.Repopath, "Kernels");

		// ----- ----- CONSTRUCTORS ----- ----- \\
		public GuiBuilder(string repopath, ListBox listBox_log, OpenClService contextH, Panel panel_kernel, CheckBox? silenceCheckBox = null)
		{
			this.Repopath = repopath;
			this.LogList = listBox_log;
			this.ContextH = contextH;
			this.ArgumentsPanel = panel_kernel;
			this.SilenceCheck = silenceCheckBox ?? new CheckBox();

			// Register events
			this.ArgumentsPanel.MouseDoubleClick += (s, e) => this.BuildPanel();
		}





		// ----- ----- METHODS ----- ----- \\
		public string Log(string message = "", string inner = "", int indent = 0)
		{
			string indentString = new('~', indent);
			string logMessage = $"[Gui] {indentString}{message} ({inner})";
			this.LogList.Items.Add(logMessage);
			this.LogList.TopIndex = this.LogList.Items.Count - 1;
			return logMessage;
		}


		public void BuildPanel(float inputWidthPart = 0.55f, bool optionalArgsOnly = false)
		{
			// Clear panel & get dimensions
			this.ArgumentsPanel.Controls.Clear();
			this.NumericsList.Clear();
			this.LabelList.Clear();
			int maxWidth = this.ArgumentsPanel.Width;
			int maxHeight = this.ArgumentsPanel.Height;
			int inputWidth = (int) (maxWidth * inputWidthPart);

			// Get kernelArgs
			Dictionary<string, Type> arguments = this.ContextH.KernelHandling?.GetKernelArguments() ?? [];
			var allArgNames = arguments.Keys.ToList();

			// First pass: Create all non-RGB controls
			int y = 10;
			for (int i = 0; i < allArgNames.Count; i++)
			{
				string argName = allArgNames[i];
				Type argType = arguments[argName];

				// Skip if this is part of an RGB triplet (we'll handle them together)
				if (argName.EndsWith("R") && i + 2 < allArgNames.Count &&
					allArgNames[i + 1] == argName.Substring(0, argName.Length - 1) + "G" &&
					allArgNames[i + 2] == argName.Substring(0, argName.Length - 1) + "B")
				{
					continue;
				}
				if (argName.EndsWith("G") || argName.EndsWith("B"))
				{
					continue;
				}

				// Create numeric input
				NumericUpDown numeric = new()
				{
					Name = $"input_arg_{argName}",
					Location = new Point(maxWidth - inputWidth, y),
					Size = new Size(inputWidth - 20, 23),
					Minimum = this.GetMinimumValue(argType),
					Maximum = this.GetMaximumValue(argType),
					Value = this.GetDefaultValue(argName, argType),
					DecimalPlaces = argType == typeof(float) ? 4 :
								  argType == typeof(double) ? 8 :
								  argType == typeof(decimal) ? 12 : 0,
					Increment = this.GetIncrementValue(argType)
				};

				// Special formatting
				if (argType.Name.EndsWith("*"))
				{
					numeric.BackColor = Color.LightCoral;
					numeric.Enabled = false;

					if (optionalArgsOnly)
					{
						this.NumericsList.Add(numeric);
						continue;
					}
				}
				else if (this.IsSpecialParameter(argName))
				{
					numeric.BackColor = Color.LightGoldenrodYellow;
					numeric.ReadOnly = true;
					numeric.Click += (s, e) =>
					{
						if (Control.ModifierKeys == Keys.Control)
						{
							numeric.ReadOnly = !numeric.ReadOnly;
						}
					};

					if (optionalArgsOnly)
					{
						this.NumericsList.Add(numeric);
						continue;
					}
				}

				// Create label
				Label label = new()
				{
					Name = $"label_arg_{argName}",
					Text = argName,
					Location = new Point(10, y),
					Size = new Size(maxWidth - 25 - inputWidth, 23)
				};
				this.ArgumentsPanel.Controls.Add(label);
				this.LabelList.Add(label);

				this.ArgumentsPanel.Controls.Add(numeric);
				this.NumericsList.Add(numeric);

				// Create tooltip
				ToolTip toolTip = new();
				toolTip.SetToolTip(numeric, $"Type: {argType.Name}\n");




				y += 30;
			}

			// Second pass: Create color pickers for RGB triplets
			for (int i = 0; i < allArgNames.Count - 2; i++)
			{
				string name1 = allArgNames[i];
				string name2 = allArgNames[i + 1];
				string name3 = allArgNames[i + 2];

				if (name1.EndsWith("R") && name2.EndsWith("G") && name3.EndsWith("B") &&
					name1.Substring(0, name1.Length - 1) == name2.Substring(0, name2.Length - 1) &&
					name2.Substring(0, name2.Length - 1) == name3.Substring(0, name3.Length - 1))
				{
					string colorName = name1.Substring(0, name1.Length - 1);

					// Create color picker button
					Button colorButton = new()
					{
						Name = $"button_arg_{colorName}",
						Text = $"{colorName} Color",
						Location = new Point(maxWidth - inputWidth, y),
						Size = new Size(inputWidth - 25, 23),
						BackColor = Color.FromArgb(
							(int) this.GetDefaultValue(name1, typeof(int)),
							(int) this.GetDefaultValue(name2, typeof(int)),
							(int) this.GetDefaultValue(name3, typeof(int))),
						Tag = new string[] { name1, name2, name3 } // Store component names
					};
					this.UpdateButtonTextColor(colorButton);

					colorButton.Click += (s, e) =>
					{
						ColorDialog cd = new() { Color = colorButton.BackColor };
						if (cd.ShowDialog() == DialogResult.OK)
						{
							colorButton.BackColor = cd.Color;
							this.UpdateButtonTextColor(colorButton);
						}
					};

					// Create label
					Label colorLabel = new()
					{
						Name = $"label_arg_{colorName}",
						Text = colorName,
						Location = new Point(10, y),
						Size = new Size(maxWidth - 20 - inputWidth, 23)
					};

					this.ArgumentsPanel.Controls.Add(colorLabel);
					this.ArgumentsPanel.Controls.Add(colorButton);
					this.LabelList.Add(colorLabel);
					y += 30;

					// Skip next two elements (G and B)
					i += 2;
				}
			}

			// Add vertical scrollbar if needed
			this.ArgumentsPanel.AutoScroll = y > maxHeight;

		}

		public object[] GetArgumentValues()
		{
			var argsDefinitions = this.ContextH.KernelHandling?.GetKernelArguments() ?? [];
			object[] values = new object[argsDefinitions.Count];
			var allArgNames = argsDefinitions.Keys.ToList();

			for (int i = 0; i < allArgNames.Count; i++)
			{
				string argName = allArgNames[i];
				Type argType = argsDefinitions[argName];

				// Handle RGB components
				if (argName.EndsWith("R") && i + 2 < allArgNames.Count &&
					allArgNames[i + 1].EndsWith("G") && allArgNames[i + 2].EndsWith("B") &&
					argName.Substring(0, argName.Length - 1) ==
					allArgNames[i + 1].Substring(0, allArgNames[i + 1].Length - 1))
				{
					string buttonName = $"button_arg_{argName.Substring(0, argName.Length - 1)}";
					var button = this.ArgumentsPanel.Controls.OfType<Button>()
								   .FirstOrDefault(b => b.Name == buttonName);

					if (button != null)
					{
						Color c = button.BackColor;
						values[i] = (int) c.B;
						values[i + 1] = (int) c.G;
						values[i + 2] = (int) c.R;
						i += 2; // Skip G and B components
						continue;
					}
				}

				// Handle normal numeric inputs
				var numeric = this.NumericsList.FirstOrDefault(n =>
					n.Name == $"input_arg_{argName}");

				if (numeric != null)
				{
					if (argType.Name.EndsWith("*"))
					{
						values[i] = new IntPtr(Convert.ToInt64(numeric.Value));
					}
					else if (argType == typeof(char))
					{
						values[i] = Convert.ToChar(Convert.ToInt32(numeric.Value));
					}
					else if (argType == typeof(byte))
					{
						values[i] = Convert.ToByte(numeric.Value);
					}
					else if (argType == typeof(sbyte))
					{
						values[i] = Convert.ToSByte(numeric.Value);
					}
					else if (argType == typeof(short))
					{
						values[i] = Convert.ToInt16(numeric.Value);
					}
					else if (argType == typeof(ushort))
					{
						values[i] = Convert.ToUInt16(numeric.Value);
					}
					else if (argType == typeof(int))
					{
						values[i] = Convert.ToInt32(numeric.Value);
					}
					else if (argType == typeof(uint))
					{
						values[i] = Convert.ToUInt32(numeric.Value);
					}
					else if (argType == typeof(long))
					{
						values[i] = Convert.ToInt64(numeric.Value);
					}
					else if (argType == typeof(ulong))
					{
						values[i] = Convert.ToUInt64(numeric.Value);
					}
					else if (argType == typeof(float))
					{
						values[i] = Convert.ToSingle(numeric.Value);
					}
					else if (argType == typeof(double))
					{
						values[i] = Convert.ToDouble(numeric.Value);
					}
					else if (argType == typeof(decimal))
					{
						values[i] = numeric.Value;
					}
					else
					{
						values[i] = Convert.ChangeType(numeric.Value, argType);
					}
				}
			}

			return values;
		}

		public string[] GetArgumentNames()
		{
			var argsDefinitions = this.ContextH.KernelHandling?.GetKernelArguments() ?? [];
			return argsDefinitions.Keys.ToArray();
		}

		private decimal GetMinimumValue(Type type) => type == typeof(char) ? byte.MinValue :
				   type == typeof(sbyte) ? sbyte.MinValue :
				   type == typeof(short) ? short.MinValue :
				   type == typeof(ushort) ? ushort.MinValue :
				   type == typeof(int) ? int.MinValue :
				   type == typeof(uint) ? uint.MinValue :
				   type == typeof(long) ? long.MinValue :
				   type == typeof(ulong) ? ulong.MinValue :
				   type == typeof(float) ? decimal.MinValue :
				   type == typeof(double) ? decimal.MinValue :
				   type == typeof(decimal) ? decimal.MinValue : 0;

		private decimal GetMaximumValue(Type type) => type == typeof(char) ? byte.MaxValue :
				   type == typeof(sbyte) ? sbyte.MaxValue :
				   type == typeof(short) ? short.MaxValue :
				   type == typeof(ushort) ? ushort.MaxValue :
				   type == typeof(int) ? int.MaxValue :
				   type == typeof(uint) ? uint.MaxValue :
				   type == typeof(long) ? long.MaxValue :
				   type == typeof(ulong) ? ulong.MaxValue :
				   type == typeof(float) ? decimal.MaxValue :
				   type == typeof(double) ? decimal.MaxValue :
				   type == typeof(decimal) ? decimal.MaxValue : long.MaxValue;

		private decimal GetDefaultValue(string argName, Type argType)
		{
			if (argType.Name.EndsWith("*"))
			{
				return IntPtr.Zero;
			}

			if (argName.Contains("width", StringComparison.OrdinalIgnoreCase))
			{
				return 0;
			}

			if (argName.Contains("height", StringComparison.OrdinalIgnoreCase))
			{
				return 0;
			}

			if (argName.Contains("channel", StringComparison.OrdinalIgnoreCase))
			{
				return 4;
			}

			return argName.Contains("bit", StringComparison.OrdinalIgnoreCase) ? 8 : 0;
		}

		private decimal GetIncrementValue(Type type) => type == typeof(float) ? 0.01m :
				   type == typeof(double) ? 0.0001m :
				   type == typeof(decimal) ? 0.000001m : 1;

		private bool IsSpecialParameter(string argName) => argName.Contains("width", StringComparison.OrdinalIgnoreCase) ||
				   argName.Contains("height", StringComparison.OrdinalIgnoreCase) ||
				   argName.Contains("channel", StringComparison.OrdinalIgnoreCase) ||
				   argName.Contains("bit", StringComparison.OrdinalIgnoreCase);

		private void UpdateButtonTextColor(Button button) => button.ForeColor = button.BackColor.GetBrightness() < 0.5 ? Color.White : Color.Black;





		public void RenderOverlayInPicturebox(PictureBox pbox, Dictionary<string, object> values, int fontSize = 10, Color? color = null, Size? size = null, Point? point = null)
		{
			if (pbox.Image is not Bitmap image)
			{
				this.Log("Image was null", "", 1);
				return;
			}

			color ??= Color.White;
			point ??= new Point(10, 10);
			fontSize = fontSize <= 0 ? image.Height / 48 : fontSize;

			// Berechne Box-Größe automatisch, falls nicht gesetzt
			if (size == null)
			{
				int lineHeight = fontSize + 2;
				int height = lineHeight * values.Count;
				int width = values.Select(kv => TextRenderer.MeasureText($"{kv.Key}: {kv.Value}", new Font("Arial", fontSize, FontStyle.Bold)).Width).Max() + 4;
				size = new Size(Math.Min(width, image.Width / 3), Math.Min(height, image.Height / 3) + 20);
			}

			Bitmap overlay = new(size.Value.Width, size.Value.Height);
			using (Graphics g = Graphics.FromImage(overlay))
			{
				g.Clear(Color.FromArgb(64, 0, 0, 0)); // leicht transparenter Hintergrund für Lesbarkeit
				g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
				g.SmoothingMode = SmoothingMode.HighQuality;
				g.InterpolationMode = InterpolationMode.NearestNeighbor;

				Font font = new("Arial", fontSize, FontStyle.Bold);
				Brush brush = new SolidBrush(color.Value);

				int y = 0;
				foreach (var kv in values)
				{
					string text = $"{kv.Key}: {kv.Value}";
					g.DrawString(text, font, brush, 4, y);
					y += fontSize + 2;
				}
			}

			using (Graphics g = Graphics.FromImage(image))
			{
				g.DrawImageUnscaled(overlay, point.Value);
			}

			pbox.Image = image;
			pbox.Refresh();
		}

		public Bitmap CreateOverlayBitmap(Size? size, Dictionary<string, object> values, int fontSize = 10, Color? color = null, Size? imageSize = null)
		{
			color ??= Color.White;

			// Wenn fontSize <= 0, dann automatisch anhand imageSize berechnen, falls imageSize gesetzt
			if (fontSize <= 0 && imageSize.HasValue)
			{
				fontSize = imageSize.Value.Height / 48;
			}

			// Automatische Größe bestimmen, wenn nicht gesetzt
			if (size == null)
			{
				int effectiveFontSize = fontSize <= 0 ? 12 : fontSize;
				int lineHeight = effectiveFontSize + 2;
				int height = lineHeight * values.Count;
				int width = values.Select(kv => TextRenderer.MeasureText($"{kv.Key}: {kv.Value}", new Font("Arial", effectiveFontSize, FontStyle.Bold)).Width).Max() + 8;

				if (imageSize.HasValue)
				{
					width = Math.Min(width, imageSize.Value.Width / 3);
					height = Math.Min(height, imageSize.Value.Height / 3) + 20;
				}
				size = new Size(width, height);
			}

			Bitmap overlay = new(size.Value.Width, size.Value.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			using (Graphics g = Graphics.FromImage(overlay))
			{
				g.Clear(Color.FromArgb(64, 0, 0, 0)); // leicht transparenter schwarzer Hintergrund

				g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
				g.SmoothingMode = SmoothingMode.HighQuality;
				g.InterpolationMode = InterpolationMode.NearestNeighbor;

				using Font font = new("Arial", fontSize, FontStyle.Bold);
				using Brush brush = new SolidBrush(color.Value);

				int y = 4;
				foreach (var kv in values)
				{
					string text = $"{kv.Key}: {kv.Value}";
					g.DrawString(text, font, brush, 4, y);
					y += fontSize + 2;
				}
			}

			return overlay;
		}




		private Control AddControlToForm<T, T1>(Form form, ref int currentY, string text = "", float labelWidthPart = 0.4f) where T : Control where T1 : unmanaged
		{
			int labelWidth = (int) (form.Size.Width * labelWidthPart);

			Label label = new()
			{
				Name = $"label_{form.Name}_{text}",
				Text = text,
				Location = new Point(10, currentY),
				Size = new Size(labelWidth - 20, 23)
			};

			Control? control = null;
			if (typeof(T) == typeof(TextBox))
			{
				control = new TextBox()
				{
					Name = $"textBox_{form.Name}_{text}",
					Location = new Point(labelWidth, currentY),
					Size = new Size(form.Size.Width - labelWidth - 30, 23),
					PlaceholderText = "enter text here"
				};
			}
			else if (typeof(T) == typeof(Button))
			{
				control = new Button()
				{
					Name = $"button_{form.Name}_{text}",
					Text = text,
					Location = new Point(labelWidth, currentY),
					Size = new Size(form.Size.Width - labelWidth - 40, 23),
					BackColor = Color.Black
				};
			}
			else
			{
				control = new NumericUpDown()
				{
					Name = $"numeric_{form.Name}_{text}",
					Location = new Point(labelWidth, currentY),
					Size = new Size(form.Size.Width - labelWidth - 20, 23),
					Minimum = 0,
					Maximum = 8192,
					Value = typeof(T1) == typeof(float) ? 1.0m :
							typeof(T1) == typeof(double) ? 1.1m :
							typeof(T1) == typeof(decimal) ? 1.2m :
							text.ToLower().Contains("width") ? 512 :
							text.ToLower().Contains("height") ? 512 :
							text.ToLower().Contains("fps") ? 10 :
							text.ToLower().Contains("steps") ? 16 :
							text.ToLower().Contains("iter") ? 8 : 0,
					Increment = typeof(T1) == typeof(float) ? 0.01m :
							  typeof(T1) == typeof(double) ? 0.0001m :
							  typeof(T1) == typeof(decimal) ? 0.000001m : 1,
					DecimalPlaces = typeof(T1) == typeof(float) ? 4 :
								  typeof(T1) == typeof(double) ? 8 :
								  typeof(T1) == typeof(decimal) ? 12 : 0
				};
			}

			form.Controls.Add(label);
			form.Controls.Add(control);
			currentY += 30;

			return control!;
		}


	}
}