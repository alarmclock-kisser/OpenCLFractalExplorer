namespace OpenCLAsyncLibrary
{
	partial class MainWindowMax
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.comboBox_devices = new ComboBox();
			this.button_info = new Button();
			this.comboBox1 = new ComboBox();
			this.comboBox2 = new ComboBox();
			this.button_load = new Button();
			this.button_execute = new Button();
			this.groupBox_kernel = new GroupBox();
			this.listBox_log = new ListBox();
			this.label_info_fps = new Label();
			this.numericUpDown_fps = new NumericUpDown();
			this.button_export = new Button();
			this.button_import = new Button();
			this.button_create = new Button();
			this.groupBox_control = new GroupBox();
			this.listBox_images = new ListBox();
			this.numericUpDown_zoom = new NumericUpDown();
			this.label_info_zoom = new Label();
			this.panel_view = new Panel();
			this.pictureBox_view = new PictureBox();
			this.progressBar_vram = new ProgressBar();
			this.label_info_zoomFactor = new Label();
			this.numericUpDown_zoomFactor = new NumericUpDown();
			this.checkBox_execAsync = new CheckBox();
			this.label_execTime = new Label();
			this.checkBox_record = new CheckBox();
			this.label_meta = new Label();
			this.label_cached = new Label();
			this.progressBar_loading = new ProgressBar();
			this.checkBox_invariables = new CheckBox();
			this.checkBox_crosshair = new CheckBox();
			this.checkBox_silent = new CheckBox();
			this.checkBox_darkMode = new CheckBox();
			this.panel_kernelArgs = new Panel();
			this.groupBox_kernel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize) this.numericUpDown_fps).BeginInit();
			this.groupBox_control.SuspendLayout();
			((System.ComponentModel.ISupportInitialize) this.numericUpDown_zoom).BeginInit();
			this.panel_view.SuspendLayout();
			((System.ComponentModel.ISupportInitialize) this.pictureBox_view).BeginInit();
			((System.ComponentModel.ISupportInitialize) this.numericUpDown_zoomFactor).BeginInit();
			this.SuspendLayout();
			// 
			// comboBox_devices
			// 
			this.comboBox_devices.FormattingEnabled = true;
			this.comboBox_devices.Location = new Point(12, 12);
			this.comboBox_devices.Name = "comboBox_devices";
			this.comboBox_devices.Size = new Size(673, 23);
			this.comboBox_devices.TabIndex = 0;
			// 
			// button_info
			// 
			this.button_info.Location = new Point(691, 12);
			this.button_info.Name = "button_info";
			this.button_info.Size = new Size(23, 23);
			this.button_info.TabIndex = 1;
			this.button_info.Text = "i";
			this.button_info.UseVisualStyleBackColor = true;
			// 
			// comboBox1
			// 
			this.comboBox1.FormattingEnabled = true;
			this.comboBox1.Location = new Point(6, 22);
			this.comboBox1.Name = "comboBox1";
			this.comboBox1.Size = new Size(420, 23);
			this.comboBox1.TabIndex = 2;
			// 
			// comboBox2
			// 
			this.comboBox2.FormattingEnabled = true;
			this.comboBox2.Location = new Point(432, 22);
			this.comboBox2.Name = "comboBox2";
			this.comboBox2.Size = new Size(70, 23);
			this.comboBox2.TabIndex = 3;
			// 
			// button_load
			// 
			this.button_load.Location = new Point(508, 22);
			this.button_load.Name = "button_load";
			this.button_load.Size = new Size(60, 23);
			this.button_load.TabIndex = 4;
			this.button_load.Text = "Load";
			this.button_load.UseVisualStyleBackColor = true;
			// 
			// button_execute
			// 
			this.button_execute.Location = new Point(574, 22);
			this.button_execute.Name = "button_execute";
			this.button_execute.Size = new Size(75, 23);
			this.button_execute.TabIndex = 5;
			this.button_execute.Text = "Execute";
			this.button_execute.UseVisualStyleBackColor = true;
			// 
			// groupBox_kernel
			// 
			this.groupBox_kernel.Controls.Add(this.panel_kernelArgs);
			this.groupBox_kernel.Controls.Add(this.comboBox1);
			this.groupBox_kernel.Controls.Add(this.button_execute);
			this.groupBox_kernel.Controls.Add(this.comboBox2);
			this.groupBox_kernel.Controls.Add(this.button_load);
			this.groupBox_kernel.Location = new Point(1232, 12);
			this.groupBox_kernel.Name = "groupBox_kernel";
			this.groupBox_kernel.Size = new Size(660, 582);
			this.groupBox_kernel.TabIndex = 6;
			this.groupBox_kernel.TabStop = false;
			this.groupBox_kernel.Text = "OpenCL-Kernel";
			// 
			// listBox_log
			// 
			this.listBox_log.FormattingEnabled = true;
			this.listBox_log.ItemHeight = 15;
			this.listBox_log.Location = new Point(12, 625);
			this.listBox_log.Name = "listBox_log";
			this.listBox_log.Size = new Size(1880, 184);
			this.listBox_log.TabIndex = 7;
			// 
			// label_info_fps
			// 
			this.label_info_fps.AutoSize = true;
			this.label_info_fps.Location = new Point(68, 175);
			this.label_info_fps.Name = "label_info_fps";
			this.label_info_fps.Size = new Size(26, 15);
			this.label_info_fps.TabIndex = 31;
			this.label_info_fps.Text = "FPS";
			// 
			// numericUpDown_fps
			// 
			this.numericUpDown_fps.Location = new Point(98, 171);
			this.numericUpDown_fps.Maximum = new decimal(new int[] { 144, 0, 0, 0 });
			this.numericUpDown_fps.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
			this.numericUpDown_fps.Name = "numericUpDown_fps";
			this.numericUpDown_fps.Size = new Size(35, 23);
			this.numericUpDown_fps.TabIndex = 30;
			this.numericUpDown_fps.Value = new decimal(new int[] { 10, 0, 0, 0 });
			// 
			// button_export
			// 
			this.button_export.Location = new Point(139, 171);
			this.button_export.Name = "button_export";
			this.button_export.Size = new Size(55, 23);
			this.button_export.TabIndex = 29;
			this.button_export.Text = "Export";
			this.button_export.UseVisualStyleBackColor = true;
			// 
			// button_import
			// 
			this.button_import.Location = new Point(139, 142);
			this.button_import.Name = "button_import";
			this.button_import.Size = new Size(55, 23);
			this.button_import.TabIndex = 28;
			this.button_import.Text = "Import";
			this.button_import.UseVisualStyleBackColor = true;
			// 
			// button_create
			// 
			this.button_create.Location = new Point(129, 22);
			this.button_create.Name = "button_create";
			this.button_create.Size = new Size(65, 23);
			this.button_create.TabIndex = 27;
			this.button_create.Text = "Create";
			this.button_create.UseVisualStyleBackColor = true;
			// 
			// groupBox_control
			// 
			this.groupBox_control.Controls.Add(this.label_execTime);
			this.groupBox_control.Controls.Add(this.checkBox_execAsync);
			this.groupBox_control.Controls.Add(this.label_info_zoomFactor);
			this.groupBox_control.Controls.Add(this.label_info_zoom);
			this.groupBox_control.Controls.Add(this.checkBox_record);
			this.groupBox_control.Controls.Add(this.numericUpDown_zoomFactor);
			this.groupBox_control.Controls.Add(this.label_cached);
			this.groupBox_control.Controls.Add(this.numericUpDown_zoom);
			this.groupBox_control.Controls.Add(this.button_export);
			this.groupBox_control.Controls.Add(this.button_create);
			this.groupBox_control.Controls.Add(this.label_info_fps);
			this.groupBox_control.Controls.Add(this.button_import);
			this.groupBox_control.Controls.Add(this.numericUpDown_fps);
			this.groupBox_control.Location = new Point(1026, 392);
			this.groupBox_control.Name = "groupBox_control";
			this.groupBox_control.Size = new Size(200, 202);
			this.groupBox_control.TabIndex = 32;
			this.groupBox_control.TabStop = false;
			this.groupBox_control.Text = "Control";
			// 
			// listBox_images
			// 
			this.listBox_images.FormattingEnabled = true;
			this.listBox_images.ItemHeight = 15;
			this.listBox_images.Location = new Point(717, 410);
			this.listBox_images.Name = "listBox_images";
			this.listBox_images.Size = new Size(300, 184);
			this.listBox_images.TabIndex = 33;
			// 
			// numericUpDown_zoom
			// 
			this.numericUpDown_zoom.Location = new Point(6, 171);
			this.numericUpDown_zoom.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
			this.numericUpDown_zoom.Minimum = new decimal(new int[] { 3, 0, 0, 0 });
			this.numericUpDown_zoom.Name = "numericUpDown_zoom";
			this.numericUpDown_zoom.Size = new Size(56, 23);
			this.numericUpDown_zoom.TabIndex = 32;
			this.numericUpDown_zoom.Value = new decimal(new int[] { 3, 0, 0, 0 });
			// 
			// label_info_zoom
			// 
			this.label_info_zoom.AutoSize = true;
			this.label_info_zoom.Location = new Point(6, 153);
			this.label_info_zoom.Name = "label_info_zoom";
			this.label_info_zoom.Size = new Size(39, 15);
			this.label_info_zoom.TabIndex = 33;
			this.label_info_zoom.Text = "Zoom";
			// 
			// panel_view
			// 
			this.panel_view.Controls.Add(this.pictureBox_view);
			this.panel_view.Location = new Point(12, 59);
			this.panel_view.Name = "panel_view";
			this.panel_view.Size = new Size(702, 535);
			this.panel_view.TabIndex = 35;
			// 
			// pictureBox_view
			// 
			this.pictureBox_view.Location = new Point(3, 3);
			this.pictureBox_view.Name = "pictureBox_view";
			this.pictureBox_view.Size = new Size(696, 529);
			this.pictureBox_view.TabIndex = 8;
			this.pictureBox_view.TabStop = false;
			// 
			// progressBar_vram
			// 
			this.progressBar_vram.Location = new Point(12, 41);
			this.progressBar_vram.Name = "progressBar_vram";
			this.progressBar_vram.Size = new Size(702, 12);
			this.progressBar_vram.TabIndex = 34;
			// 
			// label_info_zoomFactor
			// 
			this.label_info_zoomFactor.AutoSize = true;
			this.label_info_zoomFactor.Location = new Point(7, 109);
			this.label_info_zoomFactor.Name = "label_info_zoomFactor";
			this.label_info_zoomFactor.Size = new Size(49, 15);
			this.label_info_zoomFactor.TabIndex = 42;
			this.label_info_zoomFactor.Text = "Zoom f.";
			// 
			// numericUpDown_zoomFactor
			// 
			this.numericUpDown_zoomFactor.DecimalPlaces = 4;
			this.numericUpDown_zoomFactor.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
			this.numericUpDown_zoomFactor.Location = new Point(7, 127);
			this.numericUpDown_zoomFactor.Maximum = new decimal(new int[] { 128, 0, 0, 0 });
			this.numericUpDown_zoomFactor.Name = "numericUpDown_zoomFactor";
			this.numericUpDown_zoomFactor.Size = new Size(55, 23);
			this.numericUpDown_zoomFactor.TabIndex = 41;
			this.numericUpDown_zoomFactor.Value = new decimal(new int[] { 11, 0, 0, 65536 });
			// 
			// checkBox_execAsync
			// 
			this.checkBox_execAsync.AutoSize = true;
			this.checkBox_execAsync.Location = new Point(6, 62);
			this.checkBox_execAsync.Name = "checkBox_execAsync";
			this.checkBox_execAsync.Size = new Size(58, 19);
			this.checkBox_execAsync.TabIndex = 40;
			this.checkBox_execAsync.Text = "Async";
			this.checkBox_execAsync.UseVisualStyleBackColor = true;
			// 
			// label_execTime
			// 
			this.label_execTime.AutoSize = true;
			this.label_execTime.Location = new Point(7, 84);
			this.label_execTime.Name = "label_execTime";
			this.label_execTime.Size = new Size(41, 15);
			this.label_execTime.TabIndex = 39;
			this.label_execTime.Text = "Exec: -";
			// 
			// checkBox_record
			// 
			this.checkBox_record.AutoSize = true;
			this.checkBox_record.Location = new Point(6, 22);
			this.checkBox_record.Name = "checkBox_record";
			this.checkBox_record.Size = new Size(63, 19);
			this.checkBox_record.TabIndex = 38;
			this.checkBox_record.Text = "Record";
			this.checkBox_record.UseVisualStyleBackColor = true;
			// 
			// label_meta
			// 
			this.label_meta.AutoSize = true;
			this.label_meta.Location = new Point(720, 392);
			this.label_meta.Name = "label_meta";
			this.label_meta.Size = new Size(101, 15);
			this.label_meta.TabIndex = 37;
			this.label_meta.Text = "No image loaded.";
			// 
			// label_cached
			// 
			this.label_cached.AutoSize = true;
			this.label_cached.Location = new Point(7, 44);
			this.label_cached.Name = "label_cached";
			this.label_cached.Size = new Size(51, 15);
			this.label_cached.TabIndex = 36;
			this.label_cached.Text = "Cache: -";
			// 
			// progressBar_loading
			// 
			this.progressBar_loading.Location = new Point(1352, 600);
			this.progressBar_loading.Name = "progressBar_loading";
			this.progressBar_loading.Size = new Size(540, 19);
			this.progressBar_loading.TabIndex = 42;
			// 
			// checkBox_invariables
			// 
			this.checkBox_invariables.AutoSize = true;
			this.checkBox_invariables.Location = new Point(1232, 600);
			this.checkBox_invariables.Name = "checkBox_invariables";
			this.checkBox_invariables.Size = new Size(114, 19);
			this.checkBox_invariables.TabIndex = 41;
			this.checkBox_invariables.Text = "Show invariables";
			this.checkBox_invariables.UseVisualStyleBackColor = true;
			// 
			// checkBox_crosshair
			// 
			this.checkBox_crosshair.AutoSize = true;
			this.checkBox_crosshair.Location = new Point(73, 600);
			this.checkBox_crosshair.Name = "checkBox_crosshair";
			this.checkBox_crosshair.Size = new Size(75, 19);
			this.checkBox_crosshair.TabIndex = 40;
			this.checkBox_crosshair.Text = "Crosshair";
			this.checkBox_crosshair.UseVisualStyleBackColor = true;
			// 
			// checkBox_silent
			// 
			this.checkBox_silent.AutoSize = true;
			this.checkBox_silent.Location = new Point(12, 600);
			this.checkBox_silent.Name = "checkBox_silent";
			this.checkBox_silent.Size = new Size(55, 19);
			this.checkBox_silent.TabIndex = 39;
			this.checkBox_silent.Text = "Silent";
			this.checkBox_silent.UseVisualStyleBackColor = true;
			// 
			// checkBox_darkMode
			// 
			this.checkBox_darkMode.AutoSize = true;
			this.checkBox_darkMode.Location = new Point(1026, 600);
			this.checkBox_darkMode.Name = "checkBox_darkMode";
			this.checkBox_darkMode.Size = new Size(84, 19);
			this.checkBox_darkMode.TabIndex = 38;
			this.checkBox_darkMode.Text = "Dark mode";
			this.checkBox_darkMode.UseVisualStyleBackColor = true;
			// 
			// panel_kernelArgs
			// 
			this.panel_kernelArgs.BackColor = SystemColors.ControlLight;
			this.panel_kernelArgs.Location = new Point(6, 51);
			this.panel_kernelArgs.Name = "panel_kernelArgs";
			this.panel_kernelArgs.Size = new Size(643, 525);
			this.panel_kernelArgs.TabIndex = 6;
			// 
			// MainWindowMax
			// 
			this.AutoScaleDimensions = new SizeF(7F, 15F);
			this.AutoScaleMode = AutoScaleMode.Font;
			this.ClientSize = new Size(1904, 821);
			this.Controls.Add(this.progressBar_loading);
			this.Controls.Add(this.checkBox_invariables);
			this.Controls.Add(this.checkBox_crosshair);
			this.Controls.Add(this.checkBox_silent);
			this.Controls.Add(this.checkBox_darkMode);
			this.Controls.Add(this.label_meta);
			this.Controls.Add(this.panel_view);
			this.Controls.Add(this.progressBar_vram);
			this.Controls.Add(this.listBox_images);
			this.Controls.Add(this.groupBox_control);
			this.Controls.Add(this.listBox_log);
			this.Controls.Add(this.groupBox_kernel);
			this.Controls.Add(this.button_info);
			this.Controls.Add(this.comboBox_devices);
			this.MaximumSize = new Size(1920, 860);
			this.MinimumSize = new Size(1920, 860);
			this.Name = "MainWindowMax";
			this.Text = "MainWindowMax";
			this.groupBox_kernel.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize) this.numericUpDown_fps).EndInit();
			this.groupBox_control.ResumeLayout(false);
			this.groupBox_control.PerformLayout();
			((System.ComponentModel.ISupportInitialize) this.numericUpDown_zoom).EndInit();
			this.panel_view.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize) this.pictureBox_view).EndInit();
			((System.ComponentModel.ISupportInitialize) this.numericUpDown_zoomFactor).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();
		}

		#endregion

		private ComboBox comboBox_devices;
		private Button button_info;
		private ComboBox comboBox1;
		private ComboBox comboBox2;
		private Button button_load;
		private Button button_execute;
		private GroupBox groupBox_kernel;
		private ListBox listBox_log;
		private Label label_info_fps;
		private NumericUpDown numericUpDown_fps;
		private Button button_export;
		private Button button_import;
		private Button button_create;
		private GroupBox groupBox_control;
		private NumericUpDown numericUpDown_zoom;
		private ListBox listBox_images;
		private Label label_info_zoom;
		private Panel panel_view;
		private PictureBox pictureBox_view;
		private ProgressBar progressBar_vram;
		private Label label_execTime;
		private CheckBox checkBox_execAsync;
		private Label label_info_zoomFactor;
		private CheckBox checkBox_record;
		private NumericUpDown numericUpDown_zoomFactor;
		private Label label_cached;
		private Label label_meta;
		private ProgressBar progressBar_loading;
		private CheckBox checkBox_invariables;
		private CheckBox checkBox_crosshair;
		private CheckBox checkBox_silent;
		private CheckBox checkBox_darkMode;
		private Panel panel_kernelArgs;
	}
}