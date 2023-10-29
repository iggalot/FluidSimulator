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

         double SPEED_FACTOR = 50;
        public double gravity = 9.81;
        Vector2 position = new Vector2(300,100);
        double delta_y = 0;

        Vector2 velocity = new Vector2(0,0);

        Vector2 DOWN = new Vector2(0, 1);
        Vector2 UP = new Vector2(0, -1);

        double drop_dia = 7.5f;


        private TimeSpan lastRender;
        double time = 0;
        double dt = 0;

        Ellipse cir = new Ellipse();

        public MainWindow()
        {
            InitializeComponent();

            lastRender = TimeSpan.FromTicks(DateTime.Now.Ticks);
            CompositionTarget.Rendering += StartAnimation;    
        }

        private void StartAnimation(object sender, EventArgs e)
        {
            RenderingEventArgs renderArgs = (RenderingEventArgs)e;
            dt = Math.Max(0, (renderArgs.RenderingTime - lastRender).TotalSeconds);  // make sure we dont;t get a negative number when it's really zero on first pass
            lastRender = renderArgs.RenderingTime;

            // For an elliptical pattern
            //double x = 180 + 150 * Math.Cos(2 * time);
            //double y = 180 + 75 * Math.Sin(2 * time);

            position.X = (float)(0.5 * MainCanvas.Width);
            delta_y += (float)(gravity * dt * SPEED_FACTOR);

            if (delta_y > MainCanvas.Height - 2*drop_dia)
            {
                position.Y = (float)(MainCanvas.Height - 2*drop_dia);
            } else
            {
                position.Y = (float)delta_y;
            }

            MainCanvas.Children.Clear();


            cir.Width = 2 * drop_dia;
            cir.Height = 2 * drop_dia;
            cir.Fill = drop_color;
            Canvas.SetLeft(cir, position.X);
            Canvas.SetTop(cir, position.Y);

            MainCanvas.Children.Add(cir);

            time += dt;
        }

        //private void timer_Tick(object sender, EventArgs e)
        //{
        //    Update();
        //}

        //public void Update()
        //{
        //    MainCanvas.Children.Clear();

        //    velocity += DOWN * gravity * SPEED_FACTOR;
        //    position += velocity * (int)deltaTime.TotalSeconds;

        //    if (position.Y > MainCanvas.Height)
        //    {
        //        position.Y = (float)MainCanvas.Height - drop_dia;
        //    }



        //    MainCanvas.Children.Add(cir);

        //}
    }
}
