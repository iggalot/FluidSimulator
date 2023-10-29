using System;
using System.Numerics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace FluidSimulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Brush drop_color = new SolidColorBrush(Color.FromRgb(0,0,220));

        double collision_damping = 1.0;  // factor for amount of rebound as a function of initial velicty.
        double SPEED_FACTOR = 800;
        public double gravity = 9.81;
        Vector2 position = new Vector2(0, 0);

        Vector2 velocity = new Vector2(0,0);
        Vector2 halfBoundBox = new Vector2(0, 0);

        Vector2 DOWN = new Vector2(0, 1);
        Vector2 RIGHT = new Vector2(1, 0);
        Vector2 UP = new Vector2(0, -1);
        Vector2 LEFT = new Vector2(-1, 0);


        double drop_dia = 7.5f;


        private TimeSpan lastRender;
        double time = 0; // total time of the simulation
        double dt = 0;

        Ellipse cir = new Ellipse();

        public MainWindow()
        {
            InitializeComponent();

            halfBoundBox.X = (float)(0.5 * MainCanvas.Width);
            halfBoundBox.Y = (float)(0.5 * MainCanvas.Height);

            lastRender = TimeSpan.FromTicks(DateTime.Now.Ticks);
            CompositionTarget.Rendering += StartAnimation;    
        }

        private void ResolveCollisions()
        {
            if ((position.Y > (MainCanvas.Height - 2 * drop_dia)))
            {
                position.Y = (float)(2.0 * halfBoundBox.Y - 2 * drop_dia);
                velocity.Y *= (float)(-1.0 * collision_damping);
            }
            if (position.Y < 0)
            {
                position.Y = 0;
                velocity.Y *= (float)(-1.0 * collision_damping);
            }

            if ((position.X > (2.0 * halfBoundBox.X - 2 * drop_dia)))
            {
                position.X = (float)(2 * halfBoundBox.X - 2 * drop_dia);
                velocity.X *= (float)(-1.0 * collision_damping);
            }
            if (position.X < 0)
            {
                position.X = 0;
                velocity.X *= (float)(-1.0 * collision_damping);
            }
        }

        private void StartAnimation(object sender, EventArgs e)
        {
            RenderingEventArgs renderArgs = (RenderingEventArgs)e;
            dt = Math.Max(0, (renderArgs.RenderingTime - lastRender).TotalSeconds);  // make sure we dont;t get a negative number when it's really zero on first pass
            lastRender = renderArgs.RenderingTime;

            velocity += LEFT * (float)(gravity * dt * SPEED_FACTOR);
            position += velocity * (float)dt;
            ResolveCollisions();

            MainCanvas.Children.Clear();

            // Draw the the circles
            cir.Width = 2.0 * drop_dia;
            cir.Height = 2.0 * drop_dia;
            cir.Fill = drop_color;
            Canvas.SetLeft(cir, position.X);
            Canvas.SetTop(cir, position.Y);

            MainCanvas.Children.Add(cir);

            time += dt;
        }
    }
}
