// Painted, battered mech armour for the procedural box/cylinder parts (MechRig).
// Everything is computed in mesh space, so the pattern rides with the part instead of swimming as the mech moves.
//  - Fake bevels: near an edge the normal tips toward the neighbouring face, so hard box edges catch the light.
//    Needs the part's shape (instance uniform `shape`, set by MechRig): xyz = box half size, w = 1;
//    or x = top radius, y = half height, z = bottom radius, w = 2 for a cylinder. w = 0 disables edge effects.
//  - Edge wear: paint chipped back to bare metal along edges, broken up by noise.
//  - Panel seams on a staggered grid, grime patches and rain streaks, fine roughness variation.
shader_type spatial;
render_mode blend_mix, cull_back, diffuse_burley, specular_schlick_ggx;

uniform vec3 paint_color : source_color = vec3(0.6, 0.62, 0.64);
uniform float paint_roughness : hint_range(0.0, 1.0) = 0.55;
uniform float paint_metallic : hint_range(0.0, 1.0) = 0.1;
uniform vec3 metal_color : source_color = vec3(0.4, 0.39, 0.37);
uniform float metal_roughness : hint_range(0.0, 1.0) = 0.45;
uniform float wear : hint_range(0.0, 1.0) = 0.5;
uniform float grime : hint_range(0.0, 1.0) = 0.5;
uniform vec3 grime_color : source_color = vec3(0.23, 0.19, 0.15);
uniform vec2 panel_size = vec2(1.9, 1.15);
uniform float panel_seams : hint_range(0.0, 1.0) = 1.0;
uniform float bevel = 0.06;
uniform sampler2D noise : hint_default_white, filter_linear_mipmap, repeat_enable;

instance uniform vec4 shape = vec4(0.0);

varying vec3 mesh_pos;
varying vec3 mesh_normal;

void vertex() {
	mesh_pos = VERTEX;
	mesh_normal = NORMAL;
}

vec3 tri_weights(vec3 n) {
	vec3 w = pow(abs(n), vec3(4.0));
	return w / (w.x + w.y + w.z);
}

float tri_noise(vec3 p, vec3 w, float scale) {
	return texture(noise, p.zy * scale).r * w.x + texture(noise, p.xz * scale).r * w.y + texture(noise, p.xy * scale).r * w.z;
}

// Distance to the nearest staggered panel seam in a 2D projection.
float seam(vec2 uv) {
	vec2 c = uv / panel_size;
	c.x += 0.37 * floor(c.y); // staggered rows
	vec2 f = 0.5 - abs(fract(c) - 0.5);
	return min(f.x * panel_size.x, f.y * panel_size.y);
}

void fragment() {
	vec3 p = mesh_pos;
	vec3 n = normalize(mesh_normal);
	vec3 bent = n;
	float edge = 1e3; // distance to the nearest edge (mesh units)

	if (shape.w > 0.5 && shape.w < 1.5) {
		// Box: for each axis that is not the face's own, the distance to that pair of edges.
		vec3 h = shape.xyz;
		vec3 d = h - abs(p);
		vec3 an = abs(n);
		float face = an.x > an.y ? (an.x > an.z ? 0.0 : 2.0) : (an.y > an.z ? 1.0 : 2.0);
		for (int a = 0; a < 3; a++) {
			if (float(a) == face) continue;
			float b = min(bevel, 0.35 * h[a]);
			float k = 1.0 - clamp(d[a] / b, 0.0, 1.0);
			vec3 dir = vec3(0.0);
			dir[a] = sign(p[a]);
			bent += dir * k * k;
			edge = min(edge, d[a]);
		}
	} else if (shape.w > 1.5) {
		// Cylinder along y: the rim between the side and a cap.
		float hh = shape.y;
		float r = mix(shape.z, shape.x, clamp(p.y / (2.0 * hh) + 0.5, 0.0, 1.0));
		float b = min(bevel, 0.35 * min(r, hh));
		if (abs(n.y) > 0.7) {
			float dr = r - length(p.xz);
			float k = 1.0 - clamp(dr / b, 0.0, 1.0);
			bent += vec3(p.x, 0.0, p.z) / max(length(p.xz), 1e-4) * k * k;
			edge = dr;
		} else {
			float dy = hh - abs(p.y);
			float k = 1.0 - clamp(dy / b, 0.0, 1.0);
			bent += vec3(0.0, sign(p.y), 0.0) * k * k;
			edge = dy;
		}
	}

	vec3 w = tri_weights(n);
	float big = tri_noise(p, w, 0.35);   // large patches
	float mid = tri_noise(p, w, 1.3);
	float fine = tri_noise(p, w, 5.0);

	// Edge wear: a band along edges whose width the noise breaks up; wider where the noise is high.
	float wear_width = mix(0.0, 0.05, wear) * (0.2 + 1.6 * mid);
	float chipped = (1.0 - smoothstep(0.0, wear_width, edge)) * step(0.62, fine * 0.6 + mid * 0.6);
	// Scattered chips and scratches on the faces too.
	chipped = max(chipped, step(0.97 - 0.08 * wear, fine * 0.55 + big * 0.55) * wear);

	// Panel seams in the dominant projection.
	vec2 uv = w.x > w.y ? (w.x > w.z ? p.zy : p.xy) : (w.y > w.z ? p.xz : p.xy);
	float s = seam(uv + 0.37);
	float groove = (1.0 - smoothstep(0.003, 0.012, s)) * panel_seams * step(0.08, edge);

	// Grime: patches plus streaks running down the side faces, heavier low on the part.
	float streak = texture(noise, vec2(p.x * 2.3 + p.z * 1.7, p.y * 0.25)).r;
	float side = 1.0 - abs(n.y);
	float dirt = clamp((big - 0.45) * 2.0 + (streak - 0.5) * 1.6 * side, 0.0, 1.0) * grime;

	vec3 col = paint_color * (0.92 + 0.16 * mid);
	col = mix(col, grime_color, dirt * 0.65);
	col = mix(col, metal_color * (0.85 + 0.3 * fine), chipped);
	col *= 1.0 - 0.45 * groove;

	ALBEDO = col;
	METALLIC = mix(paint_metallic, 0.8, chipped);
	ROUGHNESS = clamp(mix(paint_roughness + 0.25 * dirt, metal_roughness, chipped) + (fine - 0.5) * 0.12 + 0.3 * groove, 0.05, 1.0);
	NORMAL = normalize((VIEW_MATRIX * (MODEL_MATRIX * vec4(normalize(bent), 0.0))).xyz);
}
