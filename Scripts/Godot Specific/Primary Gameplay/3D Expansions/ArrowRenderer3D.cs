using Godot;
using System.Collections.Generic;
using System.Linq;

[Tool]
public partial class ArrowRenderer3D : MeshInstance3D {
	
	[ExportGroup("Body")]
	[Export] public float width = 0.05f;
	[Export] public float vertical_offset = 0.1f;

	[ExportGroup("Head")]
	[Export] public float head_length = 0.1f;
	[Export] public float head_width = 0.1f;

	[ExportGroup("Testing")]
	[Export] public Vector3[] test_points;
	[Export] public Color test_colour = new Color(1.0f, 0f, 1f, 1.0f);

	[ExportToolButton("Draw")] public Callable DrawButton => Callable.From(TestDraw);
	[ExportToolButton("Clear")] public Callable ClearButton => Callable.From(TestDraw);

	private StandardMaterial3D _mat;
	private List<Vector3> verts = new();
	private List<Vector2> uvs = new();
	private List<int> indices = new();

	// ----- // START // ----- //

	public override void _Ready() {
		ApplyTestingMaterial();
		ClearArrow();
	}

	public void TestDraw() {
		ApplyTestingMaterial();
		Draw(test_points.ToList());
	}

	private void ApplyTestingMaterial() {
		_mat = new StandardMaterial3D {
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			AlbedoColor = test_colour,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha
		};
	}

	// ----- // DRAW // ----- //

	public void Draw(List<Vector3> waypoints) {
		if (waypoints.Count < 2) return;

		verts.Clear();
		uvs.Clear();
		indices.Clear();

		List<Vector2> flattened_points = waypoints.Select(p => new Vector2(p.X, p.Z)).ToList();
		List<Vector2> headless_points = TrimTip(flattened_points, head_length);

		AppendShafts(flattened_points);
		MergeShafts();
		AppendHead(headless_points[^1], flattened_points[^1]);

		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
		arrays[(int)Mesh.ArrayType.TexUV]  = uvs.ToArray();
		arrays[(int)Mesh.ArrayType.Index]  = indices.ToArray();

		var array_mesh = new ArrayMesh();
		array_mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		array_mesh.SurfaceSetMaterial(0, _mat);
		Mesh = array_mesh;
	}

	public void ClearArrow() => Mesh = null;

	// ----- // HELPERS // ----- //

	private Vector3 ThreeDimensional(Vector2 p) => new Vector3(p.X, vertical_offset, p.Y);
	private Vector2 TwoDimensional(Vector3 p) => new Vector2(p.X p.Z);

	private Vector2 PerpendicularTo(Vector2 dir) => new Vector2(-dir.Y, dir.X);

	// ----- // MESH BUILDING // ----- //

	private void MergeShafts(List<Vector2> waypoints) {

		if (waypoints.Count < 3) return; // Needs more than one quad

		int quad_count = verts.Count / 4;

		GD.Print($"Quad count: {quad_count}");

		// a/b are the two end verts of the first quad and x/y are the two start verts of the connecting quad
		int a_index = -1; Vector2 a = new();
		int b_index = -1; Vector2 b = new();
		int x_index = -1; Vector2 x = new();
		int y_index = -1; Vector2 y = new();

		// a_x is the vector between a and x and normal_vector is the direction vector of the quad
		Vector2 a_x = new();
		Vector2 normal_vector = new();

		for (int quad = 0; quad < quad_count - 1; quad++) {

			a_index = quad * 4 + 2; a = TwoDimensional(verts[a_index]);
			b_index = quad * 4 + 3; b = TwoDimensional(verts[a_index]);
			x_index = quad * 4 + 4; x = TwoDimensional(verts[a_index]);
			y_index = quad * 4 + 5; y = TwoDimensional(verts[a_index]);

			GD.Print($"Quads {quad} and {quad + 1} combine edges: ({a}, {b}) and ({x}, {y})");

			normal_vector = (waypoints[quad + 1] - waypoints[quad]).Normalized();
			a_x = x - a;

			if(a_x == 0) return; // Perfectly aligned, edge case, just exit

			// There is a gap between a and x
			if (Vector2.Dot(a_x, normal_vector) > 0) {
				JoinAtIntersection(b_index, y_index);
				AddTriangle(b_index, a_index, x_index);
			}

			// There is an overlap between a and x
			else { 
				JoinAtIntersection(a_index, x_index);
				AddTriangle(a_index, b_index, y_index);
			}
		}
	}

	private void AppendShafts(List<Vector2> polyline) {

		float current_uv = 0f;

		for (int i = 0; i < polyline.Count - 1; i++) {
			Vector2 a = polyline[i];
			Vector2 b = polyline[i + 1];
			Vector2 right = PerpendicularTo((b - a).Normalized()) * width * 0.5f;
			float   next_uv = current_uv + a.DistanceTo(b) / width;

			int _base = verts.Count;

			verts.AddRange(new[] { ThreeDimensional(a - right), ThreeDimensional(a + right), ThreeDimensional(b + right), ThreeDimensional(b - right) });

			uvs.AddRange(new[] { new Vector2(0f, current_uv), new Vector2(1f, current_uv),
								 new Vector2(1f, next_uv),    new Vector2(0f, next_uv)});

			indices.AddRange(new[] { _base, _base + 1, _base + 2, 
									 _base, _base + 2, _base + 3 });

			current_uv = next_uv;
		}
	}

	private void AppendHead(Vector2 base_center, Vector2 tip) {
		Vector2 right = PerpendicularTo((tip - base_center).Normalized()) * (head_width * 0.5f);
		int _base = verts.Count;
		verts.AddRange(new[] { ThreeDimensional(tip), ThreeDimensional(base_center - right), ThreeDimensional(base_center + right) });
		uvs.AddRange(new[] { new Vector2(0.5f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) });
		indices.AddRange(new[] { _base, _base + 1, _base + 2 });
	}

	private List<Vector2> TrimTip(List<Vector2> polyline, float trim_length) {
		var   trimmed   = new List<Vector2>(polyline);
		float remaining = trim_length;

		while (trimmed.Count > 1) {
			float seg = trimmed[^1].DistanceTo(trimmed[^2]);

			if (seg <= remaining) {
				remaining -= seg;
				trimmed.RemoveAt(trimmed.Count - 1);
			}
			else {
				Vector2 dir = (trimmed[^1] - trimmed[^2]).Normalized();
				trimmed[^1] -= dir * remaining;
				break;
			}
		}

		return trimmed;
	}
}
