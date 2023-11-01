using Godot;
using System;

public partial class Game : Node2D
{
	Label label; // for displaying particle text during debugging
	Label velocity_label;

	int frame_count = 0;
	int frame_count_max = 5;

	Color[] borderColors;
	private Vector2[] borderPoints;
	const int NumberOfBorderLines = 4;
	int borderWidth = 600;
	int borderHeight = 600;

	private ColorRect[] rects;
	const int NumberOfParticles = 2000;
	int NumParticlesPerRow = 1;
	int NumParticlesPerColumn = 1;

	// directional 2D unit vectors
	Vector2 DOWN = new Vector2(0, 1);
	Vector2 RIGHT = new Vector2(1, 0);
	Vector2 UP = new Vector2(0, -1);
	Vector2 LEFT = new Vector2(-1, 0);

	Vector2[] positions;
	Vector2[] velocities;
	Vector2 halfBoundBox = new Vector2(300, 300); // for storing information about the constraint box.  Set initial values in constructor below.

	double collision_damping = 0.7;  // factor for amount of rebound as a function of initial velicty.
	double SPEED_FACTOR = 400;  // a multiplier for the speed of the animation.
	public double gravity = 10; // gravity scalar value

	double particleSpacing = 15.0;
	double particleSize = 7.5f;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		rects = new ColorRect[NumberOfParticles];

		ArrangeParticleSpacings();

		// Add a label for displaying information
		label = new Label();
		label.Position = positions[0];
		label.Text = positions[0].ToString();
		label.Size = new Vector2(40,100);		
		AddChild(label);

		// Add a velocity label for displaying information
		velocity_label = new Label();
		label.Position = new Vector2(0, 0);
		label.Text = velocities[0].ToString();
		label.Size = new Vector2(40, 100);
		AddChild(velocity_label);



		for (int i = 0; i < NumberOfParticles; i++)
		{

			ColorRect rect = (new ColorRect()
			{
				Color = Colors.Blue,
				Position = new Vector2(positions[i].X + 15 * i, positions[i].Y),
				Size = new Vector2(10, 10)

			});

			rects[i] = rect;
			AddChild(rect);
		}

		//borderPoints = new Vector2[NumberOfBorderLines];
		//borderPoints[0] = new Vector2(0, 0);
		//borderPoints[1] = new Vector2(borderWidth, 0);
		//borderPoints[2] = new Vector2(borderWidth, borderHeight);
		//borderPoints[3] = new Vector2(0, borderHeight);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

        for (int i = 0; i < positions.Length; i++)
        {
            velocities[i] += DOWN * (float)(gravity * delta * SPEED_FACTOR);
            positions[i] += velocities[i] * (float)delta;
            ResolveCollisions(ref positions, ref velocities);
        }

        for (int i = 0; i < rects.Length; i++)
        {
            rects[i].Position = positions[i];
        }

		// Update label information
		// Add a label for displaying information
		RemoveChild(label);
		label = new Label();
		label.Position = positions[0];
		label.Text = rects[0].Position.ToString();
		AddChild(label);

		// Add a velocity label for displaying information
		if (frame_count % frame_count_max == 0)
		{
			RemoveChild(velocity_label);
			velocity_label.Position = new Vector2(0, 0);
			velocity_label.Text = velocities[0].ToString();
			AddChild(velocity_label);
		}

		frame_count++;
	}

	// for the collision detection.
	private void ResolveCollisions(ref Vector2[] positions, ref Vector2[] velocities)
	{

        for (int i = 0; i < positions.Length; i++)
        {
            // check collisions with the bounding box edges
            if ((positions[i].Y > (2.0 * halfBoundBox.Y - 2 * particleSize)))
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

	private void ArrangeParticleSpacings()
	{
		NumParticlesPerRow = (int)(Math.Floor(Math.Sqrt(NumberOfParticles)));
		NumParticlesPerColumn = (NumberOfParticles - 1) / NumParticlesPerRow + 1;

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
}
