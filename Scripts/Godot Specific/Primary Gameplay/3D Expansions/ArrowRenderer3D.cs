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
	[ExportToolButton("Clear")] public Callable ClearButton => Callable.From(ClearArrow);

	private StandardMaterial3D _mat;
	private List<Vector3> verts = new();
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
		indices.Clear();

		List<Vector2> flattened_points = waypoints.Select(p => new Vector2(p.X, p.Z)).ToList();
		List<Vector2> headless_points = TrimTip(flattened_points, head_length);

		AppendShafts(headless_points);
		if(waypoints.Count > 2) // Needs more than one quad
			MergeShafts();
		AppendHead(headless_points[^1], flattened_points[^1]);

		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
		arrays[(int)Mesh.ArrayType.Index]  = indices.ToArray();

		var array_mesh = new ArrayMesh();
		array_mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		array_mesh.SurfaceSetMaterial(0, _mat);
		Mesh = array_mesh;
	}

	public void ClearArrow() => Mesh = null;

	// ----- // HELPERS // ----- //

	private Vector3 ThreeDimensional(Vector2 p) => new Vector3(p.X, vertical_offset, p.Y);
	private Vector2 TwoDimensional(Vector3 p) => new Vector2(p.X, p.Z);
	private Vector2 PerpendicularTo(Vector2 dir) => new Vector2(-dir.Y, dir.X);

	// ----- // MESH BUILDING // ----- //

	private void DrawTriangle(int index_a, int index_b, int index_c) {
		indices.AddRange(new[] { index_a, index_b, index_c });
	}

	// First iteration, adding the quad shafts //

	private void AppendShafts(List<Vector2> polyline) {

		for (int i = 0; i < polyline.Count - 1; i++) {
			Vector2 a = polyline[i];
			Vector2 b = polyline[i + 1];
			Vector2 right = PerpendicularTo((b - a).Normalized()) * width * 0.5f;

			int _base = verts.Count;

			verts.AddRange(new[] { ThreeDimensional(a - right), ThreeDimensional(a + right), ThreeDimensional(b - right), ThreeDimensional(b + right) });

			DrawTriangle(_base, _base + 1, _base + 2);
			DrawTriangle(_base + 1, _base + 2, _base + 3);
		}
	}

	// Second iteration, connecting the shafts //

	private void MergeShafts() {

		int quad_count = verts.Count / 4;

		GD.Print($"Quad count: {quad_count}");

		// a/b are the two end verts of the first quad and x/y are the two start verts of the connecting quad
		int a_index = -1;
		int b_index = -1;
		int x_index = -1;
		int y_index = -1;

		for (int quad = 0; quad < quad_count - 1; quad++) {

			a_index = quad * 4 + 2;
			b_index = quad * 4 + 3;
			x_index = quad * 4 + 4;
			y_index = quad * 4 + 5;

			GD.Print($"Quads {quad} and {quad + 1} combining edges...");

			CheckJoin(a_index, x_index, b_index, y_index, false);
		}
	}

	private void CheckJoin(int a_index, int x_index, int b_index, int y_index, bool second_attempt) {

		Vector2 a_start = TwoDimensional(verts[a_index - 2]);
		Vector2 a_end = TwoDimensional(verts[a_index]);
		Vector2 x_start = TwoDimensional(verts[x_index]);
		Vector2 x_end = TwoDimensional(verts[x_index + 2]);

		Vector2 a_vec = a_end - a_start;
		Vector2 x_vec = x_end - x_start;
		float cross = a_vec.Cross(x_vec);
		Vector2 a_x = x_start - a_start;

		GD.Print($"a_start: {a_start}, a_end: {a_end}");
		GD.Print($"x_start: {x_start}, x_end: {x_end}");

		if (Mathf.IsZeroApprox(cross)) return;

		float t = a_x.Cross(x_vec) / cross;
		float u = a_x.Cross(a_vec) / cross;

		if (t >= 0f && t <= 1f && u >= 0f && u <= 1f) {
			GD.Print("Intersection found. Merging!");
			Vector3 intersection_point = ThreeDimensional(a_start + t * a_vec);
			verts[a_index] = intersection_point;
			verts[x_index] = intersection_point;
			DrawTriangle(a_index, b_index, y_index);
		}
		else {
			if(second_attempt) {
				GD.Print("Merge Failed twice, ABORT!");
				return;
			}
			GD.Print("Merge Failed, Handing over!");
			CheckJoin(b_index, y_index, a_index, x_index, true);
		}
	}

	// Third iteration, adding the triangular head //

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

	private void AppendHead(Vector2 base_center, Vector2 tip) {
		Vector2 right = PerpendicularTo((tip - base_center).Normalized()) * (head_width * 0.5f);
		int _base = verts.Count;
		verts.AddRange(new[] { ThreeDimensional(tip), ThreeDimensional(base_center - right), ThreeDimensional(base_center + right) });
		indices.AddRange(new[] { _base, _base + 1, _base + 2 });
	}
}
