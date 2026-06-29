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

		List<Vector2> flat    = waypoints.Select(p => new Vector2(p.X, p.Z)).ToList();
		List<Vector2> trimmed = TrimTip(flat, head_length);

		verts.Clear();
		uvs.Clear();
		indices.Clear();

		AppendShaft(trimmed);
		AppendHead(trimmed[^1], flat[^1]);
		AverageQuads();

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

	private Vector3 Lift(Vector2 p) => new Vector3(p.X, vertical_offset, p.Y);

	private Vector2 Perp(Vector2 dir) => new Vector2(-dir.Y, dir.X);

	// ----- // MESH BUILDING // ----- //

	private void AppendShaft(List<Vector2> polyline) {
		float current_uv = 0f;

		for (int i = 0; i < polyline.Count - 1; i++) {
			Vector2 a = polyline[i];
			Vector2 b = polyline[i + 1];
			Vector2 right = Perp((b - a).Normalized()) * width * 0.5f;
			float   next_uv = current_uv + a.DistanceTo(b) / width;

			int _base = verts.Count;
			verts.AddRange(new[] { Lift(a - right), Lift(a + right), Lift(b + right), Lift(b - right) });
			uvs.AddRange(new[] {
				new Vector2(0f, current_uv), new Vector2(1f, current_uv),
				new Vector2(1f, next_uv),    new Vector2(0f, next_uv)
			});
			indices.AddRange(new[] { _base, _base + 1, _base + 2, _base, _base + 2, _base + 3 });

			current_uv = next_uv;
		}
	}

	private void AppendHead(Vector2 base_center, Vector2 tip) {
		Vector2 right = Perp((tip - base_center).Normalized()) * (head_width * 0.5f);

		int _base = verts.Count;
		verts.AddRange(new[] { Lift(tip), Lift(base_center - right), Lift(base_center + right) });
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

	private void AverageQuads() {

		int quad_count = (verts.Count - 3) / 4;

		for (int i = 0; i < quad_count - 1; i++) {

			int left_b = i * 4 + 3;
			int right_b = i * 4 + 2;
			int left_a_next = (i + 1) * 4 + 0;
			int right_a_next = (i + 1) * 4 + 1;

			Vector3 avg_left  = (verts[left_b]  + verts[left_a_next])  * 0.5f;
			Vector3 avg_right = (verts[right_b] + verts[right_a_next]) * 0.5f;

			verts[left_b] = avg_left;
			verts[left_a_next] = avg_left;
			verts[right_b] = avg_right;
			verts[right_a_next] = avg_right;
		}
	}
}
