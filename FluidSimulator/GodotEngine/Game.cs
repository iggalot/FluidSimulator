using Godot;
using System;
using System.Threading.Tasks;

public partial class Game : Node2D
{
	int mass = 1;
	int smoothingRadius = 10;
	float[] densities;

	public float targetDensity;
	public float pressureMultiplier;

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


	float[] particleProperties;
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
		densities = new float[NumberOfParticles];

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

	void SimulationStep(float deltaTime)
    {
		// Apply gravity and calculate densities
		Parallel.For(0, NumberOfParticles, i =>
		{
			velocities[i] += DOWN * (float)(gravity * deltaTime * SPEED_FACTOR);
			densities[i] = CalculateDensity(positions[i]);
		});

		// Calculate and apply pressure forces
		Parallel.For(0, NumberOfParticles, i =>
		{
			Vector2 pressureForce = CalculatePressureForce(i);
			Vector2 pressureAcceleration = pressureForce / densities[i];
			velocities[i] = pressureAcceleration * deltaTime;
		});

		//Update positions and resolve collisions
		Parallel.For(0, NumberOfParticles, i =>
		{
			positions[i] += velocities[i] * deltaTime;
			ResolveCollisions();
		});
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		// Perform simulation calculations
		SimulationStep((float)delta);

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
	private void ResolveCollisions()
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

	static float SmoothingKernel(float radius, float dst)
    {
		float volume = (float)(Math.PI * Math.Pow(radius, 8) / 4.0);
		float value = Math.Max(0, radius * radius - dst * dst);
		return value * value * value / volume;
    }

	static float SmoothingKernelDerivative(float dst, float radius)
    {
		if (dst >= radius) return 0;
		float f = radius * radius - dst * dst;
		float scale = (float)(-24.0 / (Math.PI * Math.Pow(radius, 8)));
		return scale * dst * f * f;
    }

	float UpdateDensities()
    {
		Parallel.For(0, NumberOfParticles, i =>
		{
			densities[i] = CalculateDensity(positions[i]);
		});

		return 0;
    }
	float CalculateDensity(Vector2 samplePoint)
    {
		float density = 0;
		const float mass = 1;

		// Loop over all particle positions
		// TODO: optimize to only look at particles inside the smoothing radius
		foreach (Vector2 position in positions)
        {
			var vec = (position - samplePoint);
			float dst = (float)Math.Sqrt(vec.X * vec.X + vec.Y * vec.Y);
			float influence = SmoothingKernel(dst, smoothingRadius);
			density += mass * influence;
        }

		return density;
    }

	void CreateParticles(int seed)
    {
		Random rng = new Random(seed);
		positions = new Vector2[NumParticlesPerColumn];
		particleProperties = new float[NumberOfParticles];

        for (int i = 0; i < positions.Length; i++)
        {
			float x = (float)((rng.NextDouble() - 0.5) * 2.0 * halfBoundBox.X);
			float y = (float)((rng.NextDouble() - 0.5) * 2.0 * halfBoundBox.Y);
			positions[i] = new Vector2(x, y);
			particleProperties[i] = ExampleFunction(positions[i]);
        }
    }

	float ExampleFunction(Vector2 pos)
    {
		return (float)Math.Cos(pos.Y - 3 + Math.Sin(pos.X));
    }

	float CalculateProperty(Vector2 samplePoint)
    {
		float property = 0;

        for (int i = 0; i < NumberOfParticles; i++)
        {
			var vec = (positions[i] - samplePoint);
			float dst = (float)Math.Sqrt(vec.X * vec.X + vec.Y * vec.Y);
			float influence = SmoothingKernel(dst, smoothingRadius);
			float density = CalculateDensity(positions[i]);
			property += particleProperties[i] * influence * mass / density;
		}

		return property;
    }

	Vector2 CalculatePropertyGradient(Vector2 samplePoint)
    {
		Vector2 propertyGradient = Vector2.Zero;

		for (int i = 0; i < NumberOfParticles; i++)
		{
			var vec = (positions[i] - samplePoint);
			float dst = (float)Math.Sqrt(vec.X * vec.X + vec.Y * vec.Y);
			Vector2 dir = (positions[i] - samplePoint) / dst;

			float slope = SmoothingKernelDerivative(dst, smoothingRadius);
			float density = densities[i];
			propertyGradient += -particleProperties[i] * dir * slope * mass / density;
		}

		return propertyGradient;
    }

	Vector2 CalculatePressureForce(int particleIndex)
	{
		Vector2 pressureForce = Vector2.Zero;

		for (int otherParticleIndex = 0; otherParticleIndex < NumberOfParticles; otherParticleIndex++)
		{
			if (particleIndex == otherParticleIndex) 
				continue;

			Vector2 offset = positions[otherParticleIndex] - positions[particleIndex];
			float dst = (float)Math.Sqrt(offset.X * offset.X + offset.Y * offset.Y);
			Vector2 dir = dst == 0 ? GetRandomDir() : offset / dst;

			float slope = SmoothingKernelDerivative(dst, smoothingRadius);
			float density = densities[otherParticleIndex];
			pressureForce += -ConvertDensityToPressure(density) * dir * slope * mass / density;
		}

		return pressureForce;
	}

	Vector2 GetRandomDir()
    {
		Random rnd = new Random();
		float x = (float)(rnd.NextDouble() * 2.0 - 1.0); // rnd float between -1 and 1
		float y = (float)(rnd.NextDouble() * 2.0 - 1.0); // rnd float between -1 and 1

		return new Vector2((float)x, (float)y);
	}


	float ConvertDensityToPressure(float density)
    {
		float densityError = density - targetDensity;
		float pressure = densityError * pressureMultiplier;
		return pressure;
    }

	public void UpdateSpatialLookup(Vector2[] points, float radius)
    {
		// Create (unorderd spatial lookup
		Parallel.For(0, points.Length, i =>
		{
			(int cellX, int CellY) = PositionToCellCoord(points[i], radius);
			uint cellKey = GetKeyFromHash(HashCell(cellX, CellY));
			spatialLookup[i] = new EntryPointNotFoundException(i, cellKey);
			startIndicies[i] = int.MaxValue;  // reset start index
		});

		// Sort by cell key
		Array.Sort(spatialLookup);

		// Calculate start indices of each unique cell key in the spatial lookup
		Parallel.For(0, points.Length, i =>
		{
			uint key = spatialLookup[i].cellKey;
			uint keyPrev = i == 0 ? uint.MaxValue : spatialLookup[i - 1].cellKey;
			if (key != keyPrev)
            {
				startIndices[key] = i;
            }
		});


    }

	// Convert a position to the coordinate of the cell it is within
	public (int x, int y) PositionToCellCoord(Vector2 point, float radius)
	{
		int cellX = (int)(point.X / radius);
		int cellY = (int)(point.Y / radius);
		return (cellX, cellY);
	}

	// Convert a cell coordinate into a single number
	// Hash collisions (differnt cells -> same value) are unavoidable, but we want to at
	// least try to minimize collisions for nearby cells.  I'm sure there are better ways,
	// but this seems to work okay.
	public uint HashCell(int cellX, int CellY)
	{
		uint a = (uint)cellX * 15823;
		uint b = (uint)CellY * 9737333;
		return a + b;
	}

	// Wrap the hash value around the length of the array (so it can be used as an index)
	public uint GetKeyFromHash(uint hash)
    {
		return hash % (uint)spatialLookup.Length;
    }
}
