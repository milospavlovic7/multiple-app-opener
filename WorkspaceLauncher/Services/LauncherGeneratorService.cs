using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WorkspaceLauncher.Helpers;
using WorkspaceLauncher.Models;

namespace WorkspaceLauncher.Services
{
    public class LauncherGeneratorService
    {
        private static readonly Lazy<LauncherGeneratorService> _instance = 
            new Lazy<LauncherGeneratorService>(() => new LauncherGeneratorService());

        public static LauncherGeneratorService Instance => _instance.Value;

        private readonly string _iconsFolder;

        private LauncherGeneratorService()
        {
            _iconsFolder = Path.Combine(PersistenceService.Instance.AppDataFolder, "Icons");
            if (!Directory.Exists(_iconsFolder))
            {
                Directory.CreateDirectory(_iconsFolder);
            }
        }

        public string GenerateShortcut(Workflow workflow, string destinationFolder)
        {
            try
            {
                string exePath = Environment.ProcessPath ?? string.Empty;
                if (string.IsNullOrEmpty(exePath))
                {
                    throw new InvalidOperationException("Could not determine current application executable path.");
                }

                // Ensure destination exists
                if (!Directory.Exists(destinationFolder))
                {
                    Directory.CreateDirectory(destinationFolder);
                }

                // Generate Icon first
                string iconPath = GenerateWorkflowIconFile(workflow);

                // Create Shortcut Path
                // Replace invalid filename characters
                string safeName = string.Join("_", workflow.Name.Split(Path.GetInvalidFileNameChars()));
                string shortcutPath = Path.Combine(destinationFolder, $"{safeName}.lnk");

                // Use COM WScript.Shell dynamically to create the shortcut
                Type shellType = Type.GetTypeFromProgID("WScript.Shell") 
                    ?? throw new InvalidOperationException("WScript.Shell COM object not found.");
                
                dynamic shell = Activator.CreateInstance(shellType) 
                    ?? throw new InvalidOperationException("Could not instantiate WScript.Shell.");
                
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                
                shortcut.TargetPath = exePath;
                shortcut.Arguments = $"--launch {workflow.Id}";
                shortcut.WorkingDirectory = Path.GetDirectoryName(exePath);
                shortcut.Description = $"Launch {workflow.Name} Workspace";
                
                if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
                {
                    shortcut.IconLocation = iconPath;
                }
                
                shortcut.Save();
                return shortcutPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating shortcut: {ex.Message}");
                throw;
            }
        }

        public string GenerateWorkflowIconFile(Workflow workflow)
        {
            try
            {
                string iconPath = Path.Combine(_iconsFolder, $"{workflow.Id}.ico");
                
                // Draw png
                byte[] pngBytes = RenderIconToPng(workflow.Icon, workflow.Color);
                
                // Save as .ico
                SavePngAsIcon(pngBytes, iconPath);
                
                return iconPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error rendering icon: {ex.Message}");
                return string.Empty;
            }
        }

        private byte[] RenderIconToPng(string iconKey, string colorHex)
        {
            int size = 256;
            
            // Create a drawing visual
            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                // Parse color
                Color color;
                try
                {
                    color = (Color)ColorConverter.ConvertFromString(colorHex);
                }
                catch
                {
                    color = Color.FromRgb(138, 43, 226); // Default purple
                }

                // Draw linear gradient background
                var backgroundBrush = new LinearGradientBrush(
                    color,
                    Color.Multiply(color, 0.7f), // slightly darker gradient
                    new Point(0, 0),
                    new Point(1, 1)
                );

                // Rounded rectangle with a professional squary radius
                drawingContext.DrawRoundedRectangle(backgroundBrush, null, new Rect(0, 0, size, size), 24, 24);

                // Draw central icon geometry
                Geometry geometry = IconHelper.GetIconGeometry(iconKey);
                var bounds = geometry.Bounds;
                
                // Scale it to fit about 120 pixels in width/height
                double maxDimension = Math.Max(bounds.Width, bounds.Height);
                if (maxDimension > 0)
                {
                    double scale = 120.0 / maxDimension;
                    
                    var transform = new TransformGroup();
                    // Shift center of bounds to (0,0)
                    transform.Children.Add(new TranslateTransform(-bounds.X - bounds.Width / 2.0, -bounds.Y - bounds.Height / 2.0));
                    // Scale
                    transform.Children.Add(new ScaleTransform(scale, scale));
                    // Move to center of 256x256
                    transform.Children.Add(new TranslateTransform(size / 2.0, size / 2.0));
                    
                    geometry.Transform = transform;

                    // Draw the geometry in crisp white
                    drawingContext.DrawGeometry(Brushes.White, null, geometry);
                }
            }

            // Render visual to RenderTargetBitmap
            var rtb = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(drawingVisual);

            // Encode PNG
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            
            using (var ms = new MemoryStream())
            {
                encoder.Save(ms);
                return ms.ToArray();
            }
        }

        private void SavePngAsIcon(byte[] pngBytes, string icoPath)
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                // ICO Header
                bw.Write((short)0); // Reserved (Must be 0)
                bw.Write((short)1); // Resource type (1 for Icon)
                bw.Write((short)1); // Number of images in file

                // Directory Entry
                bw.Write((byte)0);  // Width (0 = 256)
                bw.Write((byte)0);  // Height (0 = 256)
                bw.Write((byte)0);  // Color count (0 = >=8bpp)
                bw.Write((byte)0);  // Reserved
                bw.Write((short)1); // Color planes
                bw.Write((short)32); // Bits per pixel
                bw.Write(pngBytes.Length); // Image data size in bytes
                bw.Write(22);       // Image data offset (6 bytes header + 16 bytes directory entry)

                // Image Data (raw PNG bytes)
                bw.Write(pngBytes);

                bw.Flush();
                File.WriteAllBytes(icoPath, ms.ToArray());
            }
        }
    }
}
