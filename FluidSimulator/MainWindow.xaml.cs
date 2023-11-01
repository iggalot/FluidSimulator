using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace FluidSimulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool bFirstLoad = false;  // for determining when the window is loaded
        WriteableBitmap wb {get; set;}
        public object Graphics { get; private set; }

        Brush drop_color = new SolidColorBrush(Color.FromRgb(0,0,220));

        double collision_damping = 0.0;  // factor for amount of rebound as a function of initial velicty.
        double SPEED_FACTOR = 800;  // a multiplier for the speed of the animation.
        public double gravity = 9.81; // gravity scalar value

        // size of the particle
        int NumberOfParticles = 1;
        int NumParticlesPerRow = 1;
        int NumParticlesPerColumn = 1;
        double particleSpacing = 15.0;
        double particleSize = 7.5f;


        Vector2[] positions;
        Vector2[] velocities;
        Vector2 halfBoundBox = new Vector2(0, 0); // for storing information about the constraint box.  Set initial values in constructor below.

        // directional 2D unit vectors
        Vector2 DOWN = new Vector2(0, 1);
        Vector2 RIGHT = new Vector2(1, 0);
        Vector2 UP = new Vector2(0, -1);
        Vector2 LEFT = new Vector2(-1, 0);

        // time information
        private TimeSpan lastRender;
        double time = 0; // total time of the simulation
        double dt = 0;

        public MainWindow()
        {
            InitializeComponent();

            // set the initial values for the bounding box -- default should be the limits of the canvas, but not necessarily true
            halfBoundBox.X = (float)(0.5 * MainCanvas.Width);
            halfBoundBox.Y = (float)(0.5 * MainCanvas.Height);

//            RenderBMP();
//            img.ImageSource = wb;
        }

        private void DoRenderBMP()
        {
            if (!bFirstLoad)
                return;

            // initialize our writeable bmp
            wb = new WriteableBitmap((int)(MainCanvas.ActualWidth), (int)(MainCanvas.ActualHeight), 96, 96, PixelFormats.Bgra32, null);

            Int32Rect rect = new Int32Rect(0, 0, (int)MainCanvas.ActualWidth, (int)MainCanvas.ActualHeight);

            //Width * height *  bytes per pixel aka(32/8)
            byte[] pixels =
            new byte[(int)MainCanvas.ActualWidth * (int)MainCanvas.ActualHeight * (wb.Format.BitsPerPixel / 8)];

            Random rand = new Random();
            // Color the background of the drawing area
            for (int y = 0; y < wb.PixelHeight; y++)
            {
                for (int x = 0; x < wb.PixelWidth; x++)
                {

                    int blue = 0;
                    int green = 0;
                    int red = 0;
                    int alpha = 255;

                    int pixelOffset = CalculatePixelOffset(x, y);
                    pixels[pixelOffset] = (byte)blue;
                    pixels[pixelOffset + 1] = (byte)green;
                    pixels[pixelOffset + 2] = (byte)red;
                    pixels[pixelOffset + 3] = (byte)alpha;
                }

            }

            // Draw each particle
            foreach (var item in positions)
            {
                DrawCircleToBMP(item, ref pixels);
            }

            int stride = wb.PixelWidth * (wb.Format.BitsPerPixel / 8);
            wb.WritePixels(rect, pixels, stride, 0);

            img.Source = wb;
        }

        private void RenderBMP_Click(object sender, RoutedEventArgs e)
        {
            DoRenderBMP();
        }

        private void DrawCircleToBMP(Vector2 pos, ref byte[] pixels)
        {
            int blue = 0;
            int green = 0;
            int red = 255;
            int alpha = 255;

            for (double i = 0.0; i < 360.0; i += 0.1)
            {
                double angle = i * System.Math.PI / 180;
                int x_max = (int)(pos.X + particleSize + particleSize * Math.Cos(angle));
                int x_min = (int)(pos.X + particleSize - particleSize * Math.Cos(angle));

                int y = (int)(pos.Y + particleSize + particleSize * Math.Sin(angle));

                // check the limits
                if (x_min < 0 || x_max > MainCanvas.ActualWidth || y < 0 || y >= MainCanvas.ActualHeight)
                {
                    return; // outside limits so do nothing.
                }

                // iterate along all the pixels on a horizontal line between x_min and x_max at the height elevation of y;
                for (int m = x_min; m < x_max; m++)
                {
                    int pixelOffset = CalculatePixelOffset(m, y);
                    //if (pixelOffset + 3 > pixels.Length)
                    //    return;
                    pixels[pixelOffset] = (byte)blue;
                    pixels[pixelOffset + 1] = (byte)green;
                    pixels[pixelOffset + 2] = (byte)red;
                    pixels[pixelOffset + 3] = (byte)alpha;
                }

            }
        }

        private int CalculatePixelOffset(int x, int y)
        { //pixel with is the length of a row
          //mulitply it by what row you want to be on then add the remaining pixel to move to the right
            return ((x + (wb.PixelWidth * y)) * (wb.Format.BitsPerPixel / 8));
        }

        // for the collision detection.
        private void ResolveCollisions(ref Vector2[] positions, ref Vector2[] velocities)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                // check collisions with the bounding box edges
                if ((positions[i].Y > (MainCanvas.Height - 2 * particleSize)))
                {
                    positions[i].Y = (float)(2.0 * halfBoundBox.Y - 2 * particleSize);
                    velocities[i].Y *= (float)(-1.0 * collision_damping);
                }
                if (positions[i].Y < 0)
                {
                    positions[i].Y = 0;
                    velocities[i].Y *= (float)(-1.0 * collision_damping);
                }

                if ((positions[i].X > (2.0 * halfBoundBox.X - 2 * particleSize)))
                {
                    positions[i].X = (float)(2 * halfBoundBox.X - 2 * particleSize);
                    velocities[i].X *= (float)(-1.0 * collision_damping);
                }
                if (positions[i].X < 0)
                {
                    positions[i].X = 0;
                    velocities[i].X *= (float)(-1.0 * collision_damping);
                }
            }
        }

        private void Start()
        { 
            // set the initial render time.
            lastRender = TimeSpan.FromTicks(DateTime.Now.Ticks);

            // set the  rendering callback for the animation.
            CompositionTarget.Rendering += StartAnimation;
        }

        /// <summary>
        /// The animation callback.  Computes the particle velocity and position, checks the collision criteria, and draws the particle(s)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartAnimation(object sender, EventArgs e)
        {
            RenderingEventArgs renderArgs = (RenderingEventArgs)e;
            dt = Math.Max(0, (renderArgs.RenderingTime - lastRender).TotalSeconds);  // make sure we dont;t get a negative number when it's really zero on first pass
            lastRender = renderArgs.RenderingTime;
//            dt = 0;


            for (int i = 0; i < positions.Length; i++)
            {
                velocities[i] += DOWN * (float)(gravity * dt * SPEED_FACTOR);
                positions[i] += velocities[i] * (float)dt;
                ResolveCollisions(ref positions, ref velocities);
            }

//            DrawParticlesToCanvas();

            //// For the writeable BMP
            DoRenderBMP();

            dt += 0.0005;

            time += dt;
        }

        private void DrawParticlesToCanvas()
        {
            //MainCanvas.Children.Clear();

            //for (int i = 0; i < positions.Length; i++)
            //{
            //    // Draw the the circles
            //    Ellipse cir = new Ellipse();
            //    cir.Width = 2.0 * particleSize;
            //    cir.Height = 2.0 * particleSize;
            //    cir.Fill = drop_color;
            //    Canvas.SetLeft(cir, positions[i].X);
            //    Canvas.SetTop(cir, positions[i].Y);

            //    MainCanvas.Children.Add(cir);
            //}
        }

        private void slNumParticleValue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Do something once the values change.
            NumberOfParticles = (int)slNumParticleValue.Value;
            NumParticlesPerRow = (int)(Math.Floor(Math.Sqrt(NumberOfParticles)));
            NumParticlesPerColumn = (NumberOfParticles - 1) / NumParticlesPerRow + 1;

            ArrangeParticleSpacings();

            DrawParticlesToCanvas();

            //// For the writeable BMP
            DoRenderBMP();
        }

        private void ArrangeParticleSpacings()
        {

            if (particleSpacing < 2 * particleSize)
            {
                particleSpacing = 2 * particleSize;
            }

            // Clear our array of elements
            positions = new Vector2[NumberOfParticles];
            velocities = new Vector2[NumberOfParticles];

            for (int i = 0; i < NumberOfParticles; i++)
            {
                float x = (float)((i % NumParticlesPerRow - NumParticlesPerRow / 2.0f + 0.5f) * particleSpacing);
                float y = (float)((i / NumParticlesPerRow - NumParticlesPerColumn / 2.0f + 0.5f) * particleSpacing);

                positions[i] = new Vector2(x, y);
            }

            // Move the particle pattern to the center of the canvas
            for (int i = 0; i < NumberOfParticles; i++)
            {
                positions[i] = positions[i] + halfBoundBox;
            }
        }

        private void slGravityValue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Do something once the values change.
            gravity = (double)slGravityValue.Value;
        }

        private void slCollisionDampingValue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Do something once the values change.
            collision_damping = (double)slCollisionDampingValue.Value;
        }

        private void slParticleSizeValue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Do something once the values change.
            particleSize = (double)slParticleSizeValue.Value;

            ArrangeParticleSpacings();

            DrawParticlesToCanvas();

            //// For the writeable BMP
            DoRenderBMP();
        }

        private void slParticleSpacingValue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Do something once the values change.
            particleSpacing = (double)slParticleSpacingValue.Value;

            ArrangeParticleSpacings();

            DrawParticlesToCanvas();

            //// For the writeable BMP
            DoRenderBMP();
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            Start();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            bFirstLoad = true;
        }
    }
}
