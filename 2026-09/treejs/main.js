import * as THREE from "three";
import { OrbitControls } from "three/addons/controls/OrbitControls.js";

const scene = new THREE.Scene();
const camera = new THREE.PerspectiveCamera(60, window.innerWidth / window.innerHeight, 0.1, 100);
camera.position.set(0, 0, 3);

const renderer = new THREE.WebGLRenderer({ antialias: true });
renderer.setPixelRatio(window.devicePixelRatio);
renderer.setSize(window.innerWidth, window.innerHeight);
document.body.appendChild(renderer.domElement);

// fond équirectangulaire en pavage de Truchet (arcs de cercle aléatoires), aussi utilisé comme environnement réfléchi
function makeTruchetTexture() {
	const cells = 32, size = 64;
	const canvas = document.createElement("canvas");
	canvas.width = cells * size;
	canvas.height = (cells / 2) * size;
	const ctx = canvas.getContext("2d");
	const gradient = ctx.createLinearGradient(0, 0, 0, canvas.height);
	gradient.addColorStop(0, "#d6ecff");
	gradient.addColorStop(0.5, "#eaf5ff");
	gradient.addColorStop(1, "#b8d8f5");
	ctx.fillStyle = gradient;
	ctx.fillRect(0, 0, canvas.width, canvas.height);
	// un seul dégradé continu pour tous les arcs : pas de raccord de couleur entre cases
	const lineGradient = ctx.createLinearGradient(0, 0, canvas.width, 0);
	["#ffffff", "#7fb8ee", "#4a90d9", "#cfe6ff", "#ffffff"].forEach((c, k) => lineGradient.addColorStop(k / 4, c));
	ctx.strokeStyle = lineGradient;
	ctx.lineWidth = size * 0.16;
	ctx.lineCap = "butt";
	for (let j = 0; j < cells / 2; j++) {
		for (let i = 0; i < cells; i++) {
			const x = i * size, y = j * size;
			ctx.beginPath();
			if (Math.random() < 0.5) { // arcs centrés sur les coins haut-gauche et bas-droit
				ctx.arc(x, y, size / 2, 0, Math.PI / 2);
				ctx.stroke();
				ctx.beginPath();
				ctx.arc(x + size, y + size, size / 2, Math.PI, Math.PI * 1.5);
			} else { // arcs centrés sur les coins haut-droit et bas-gauche
				ctx.arc(x + size, y, size / 2, Math.PI / 2, Math.PI);
				ctx.stroke();
				ctx.beginPath();
				ctx.arc(x, y + size, size / 2, Math.PI * 1.5, Math.PI * 2);
			}
			ctx.stroke();
		}
	}
	const texture = new THREE.CanvasTexture(canvas);
	texture.mapping = THREE.EquirectangularReflectionMapping;
	texture.colorSpace = THREE.SRGBColorSpace;
	return texture;
}
const truchetTexture = makeTruchetTexture();
scene.background = truchetTexture;
scene.environment = truchetTexture;
scene.environmentIntensity = 0.25;
scene.backgroundIntensity = 0.35;

// modèle "outdoors" : hémisphère (ciel/sol) quasi blanc + soleil blanc + appoint ambiant
const hemiLight = new THREE.HemisphereLight(0xeef4ff, 0x8a7a66, 0.15); // ciel bleu très pâle, sol beige
scene.add(hemiLight);

// 3 lucioles colorées qui virevoltent autour du dodécaèdre
const fireflies = [
	{ color: 0xfff2cc, radius: 1.3, speed: 0.015, latAmp: 0.8, latFreq: 1.7, phase: 0.0 },
	{ color: 0xccddff, radius: 1.6, speed: -0.011, latAmp: 1.0, latFreq: 1.3, phase: 2.1 },
	{ color: 0xffccdd, radius: 1.5, speed: 0.019, latAmp: 0.6, latFreq: 2.3, phase: 4.2 }
];
for (const f of fireflies) {
	f.light = new THREE.PointLight(f.color, 8, 0, 2); // decay 2 = atténuation physique en 1/d²
	f.light.add(new THREE.Mesh(
		new THREE.SphereGeometry(0.05, 8, 8),
		new THREE.MeshBasicMaterial({ color: f.color })
	));
	scene.add(f.light);
}

const ambientLight = new THREE.AmbientLight(0xffffff, 0.04); // remonte les zones à contre-jour
scene.add(ambientLight);

const geometry = new THREE.DodecahedronGeometry(1);

// 12 pentagones × 9 sommets : une teinte par face, répartie sur le cercle HSV
const faceCount = 12;
const verticesPerFace = 9;
const vertexCount = geometry.attributes.position.count;
const colors = new Float32Array(vertexCount * 3);
const faceColor = new THREE.Color();

const colorModes = [
	{ name: "Arc-en-ciel", fn: (f) => faceColor.setHSL(f / 12, 1.0, 0.5) },
	{ name: "Or", fn: (f) => faceColor.setHSL((f * 0.618033988749895) % 1, 0.85, 0.55) },
	{ name: "Température", fn: (f) => {
		const v = new THREE.Vector3().fromBufferAttribute(geometry.attributes.position, f * 9);
		const y = v.y / 1.4;
		faceColor.setHSL(0.6 - y * 0.3, 0.8, 0.5 + y * 0.1);
	}},
	{ name: "Gemmes", fn: (f) => {
		const palette = [0xff6b6b, 0x4ecdc4, 0xffe66d, 0x95e1d3];
		faceColor.setHex(palette[f % 4]);
	}}
];

function applyColorMode(modeIndex) {
	for (let face = 0; face < faceCount; face++) {
		colorModes[modeIndex].fn(face);
		for (let i = 0; i < verticesPerFace; i++) {
			const v = face * verticesPerFace + i;
			colors[v * 3] = faceColor.r;
			colors[v * 3 + 1] = faceColor.g;
			colors[v * 3 + 2] = faceColor.b;
		}
	}
	geometry.attributes.color.needsUpdate = true;
}

geometry.setAttribute("color", new THREE.BufferAttribute(colors, 3)); // enregistrer AVANT le premier appel
applyColorMode(1); // mode « Or » par défaut
// glow par sommet : 0 = face normale, >0 = face qui émet sa propre couleur
const glow = new Float32Array(vertexCount);
geometry.setAttribute("aGlow", new THREE.BufferAttribute(glow, 1));

// bump map procédural (sommes de sinus entiers => se raccorde parfaitement)
function makeBumpTexture() {
	const size = 256;
	const canvas = document.createElement("canvas");
	canvas.width = canvas.height = size;
	const ctx = canvas.getContext("2d");
	const img = ctx.createImageData(size, size);
	const TAU = Math.PI * 2;
	for (let y = 0; y < size; y++) {
		for (let x = 0; x < size; x++) {
			const u = x / size * TAU, v = y / size * TAU;
			let h = Math.sin(3 * u + 2 * v) + Math.sin(5 * u - 4 * v + 1) + 0.6 * Math.sin(11 * u + 7 * v + 2)
				+ 0.5 * Math.sin(9 * v - 13 * u) + 0.3 * Math.sin(23 * u + 17 * v) + 0.3 * Math.sin(19 * u - 29 * v + 3);
			const g = Math.max(0, Math.min(255, 128 + h * 28));
			const k = (y * size + x) * 4;
			img.data[k] = img.data[k + 1] = img.data[k + 2] = g;
			img.data[k + 3] = 255;
		}
	}
	ctx.putImageData(img, 0, 0);
	const texture = new THREE.CanvasTexture(canvas);
	texture.wrapS = texture.wrapT = THREE.RepeatWrapping;
	return texture;
}
const bumpTexture = makeBumpTexture();

// UV planaires par face (coordonnées dans le plan du pentagone) pour que le bump soit homogène
{
	const pos = geometry.attributes.position;
	const uv = new Float32Array(vertexCount * 2);
	const a = new THREE.Vector3(), b = new THREE.Vector3(), c = new THREE.Vector3();
	for (let face = 0; face < faceCount; face++) {
		a.fromBufferAttribute(pos, face * 9);
		b.fromBufferAttribute(pos, face * 9 + 1);
		c.fromBufferAttribute(pos, face * 9 + 2);
		const n = new THREE.Vector3().crossVectors(b.clone().sub(a), c.clone().sub(a)).normalize();
		const xAxis = b.clone().sub(a).normalize();
		const yAxis = new THREE.Vector3().crossVectors(n, xAxis);
		for (let i = 0; i < 9; i++) {
			const p = new THREE.Vector3().fromBufferAttribute(pos, face * 9 + i);
			uv[(face * 9 + i) * 2] = p.dot(xAxis);
			uv[(face * 9 + i) * 2 + 1] = p.dot(yAxis);
		}
	}
	geometry.setAttribute("uv", new THREE.BufferAttribute(uv, 2));
}

const material = new THREE.MeshStandardMaterial({
	vertexColors: true,
	bumpMap: bumpTexture,
	bumpScale: 1.5,
	flatShading: true,   // garde les facettes visibles du dodécaèdre
	roughness: 0.25,
	metalness: 0.45
});

// injection custom : aGlow (attribut) -> vGlow (varying) -> émissif teinté par la couleur de la face
material.onBeforeCompile = (shader) => {
	shader.vertexShader = shader.vertexShader
		.replace("#include <common>", "#include <common>\nattribute float aGlow;\nvarying float vGlow;")
		.replace("#include <begin_vertex>", "#include <begin_vertex>\nvGlow = aGlow;");
	shader.fragmentShader = shader.fragmentShader
		.replace("#include <common>", "#include <common>\nvarying float vGlow;")
		.replace("#include <emissivemap_fragment>", "#include <emissivemap_fragment>\ntotalEmissiveRadiance += vColor.rgb * vGlow; // glow par face");
};

const mesh = new THREE.Mesh(geometry, material);
scene.add(mesh);

const edges = new THREE.LineSegments(
	new THREE.EdgesGeometry(geometry),
	new THREE.LineBasicMaterial({ color: 0x0a0a0a })
);
mesh.add(edges);

// un piston par face : base en étoile à 5 branches (pointes vers les coins du pentagone) + tige
const starMaterial = new THREE.MeshStandardMaterial({ color: 0xd4a73a, metalness: 1.0, roughness: 0.2, envMapIntensity: 1.0, bumpMap: bumpTexture, bumpScale: 1.0 });
const starShape = new THREE.Shape();
for (let i = 0; i < 10; i++) {
	const r = i % 2 === 0 ? 0.35 : 0.155;
	const a = (i * Math.PI) / 5; // première pointe sur +X local = direction d'un coin
	const x = Math.cos(a) * r, y = Math.sin(a) * r;
	if (i === 0) starShape.moveTo(x, y); else starShape.lineTo(x, y);
}
starShape.closePath();
// étoile haute de 0.5 (biseau inclus) : la majeure partie reste dans le dodécaèdre
const starHeight = 1.4;
const starGeometry = new THREE.ExtrudeGeometry(starShape, {
	depth: starHeight - 0.04, bevelEnabled: true, bevelThickness: 0.02, bevelSize: 0.02, bevelSegments: 3
}).translate(0, 0, 0.02);
const pistonRest = 0.02, pistonStroke = 1.2; // au repos, l'étoile dépasse de la face (évite le z-fighting)
const pistons = [];
const facePositions = geometry.attributes.position;
for (let face = 0; face < faceCount; face++) {
	const v0 = new THREE.Vector3().fromBufferAttribute(facePositions, face * 9);
	const v1 = new THREE.Vector3().fromBufferAttribute(facePositions, face * 9 + 1);
	const v2 = new THREE.Vector3().fromBufferAttribute(facePositions, face * 9 + 2);
	const normal = new THREE.Vector3().crossVectors(v1.clone().sub(v0), v2.clone().sub(v0)).normalize();
	if (normal.dot(v0) < 0) normal.negate();
	const center = normal.clone().multiplyScalar(normal.dot(v0));
	const xAxis = v0.clone().sub(center).normalize(); // vers un coin du pentagone
	const yAxis = new THREE.Vector3().crossVectors(normal, xAxis);

	const piston = new THREE.Group();
	piston.add(new THREE.Mesh(starGeometry, starMaterial));
	piston.matrixAutoUpdate = false;
	const basis = new THREE.Matrix4().makeBasis(xAxis, yAxis, normal);
	piston.userData = { basis, center, normal, face };
	mesh.add(piston);
	pistons.push(piston);
}

const controls = new OrbitControls(camera, renderer.domElement);
controls.enableDamping = true;

// each pentagon face is triangulated into 3 triangles (9 vertices) in a row
const raycaster = new THREE.Raycaster();
const pointer = new THREE.Vector2();
let pointerDownPosition = null;

renderer.domElement.addEventListener("pointerdown", (event) => {
	pointerDownPosition = { x: event.clientX, y: event.clientY };
});

renderer.domElement.addEventListener("pointerup", (event) => {
	const dragDistance = Math.hypot(event.clientX - pointerDownPosition.x, event.clientY - pointerDownPosition.y);
	if (dragDistance > 5) return; // ignore orbit drags

	pointer.x = (event.clientX / window.innerWidth) * 2 - 1;
	pointer.y = -(event.clientY / window.innerHeight) * 2 + 1;
	raycaster.setFromCamera(pointer, camera);

	const intersections = raycaster.intersectObject(mesh, false);
	if (intersections.length === 0) return;

	const faceVertexStart = Math.floor(intersections[0].faceIndex / 3) * 9;
	const glowAttribute = geometry.attributes.aGlow;

	// clic = glow au maximum (plafonné), décroît ensuite exponentiellement
	const glowLevel = Math.min(glowAttribute.getX(faceVertexStart) + 2.0, 4.0);
	for (let i = 0; i < 9; i++) {
		glowAttribute.setX(faceVertexStart + i, glowLevel);
	}
	glowAttribute.needsUpdate = true;
});
let isRotating = true;
const rotationButton = document.getElementById("rotationToggle");
rotationButton.addEventListener("click", () => {
	isRotating = !isRotating;
	rotationButton.textContent = isRotating ? "Arrêter la rotation" : "Démarrer la rotation";
});

let colorModeIndex = 1; // démarrage sur « Or »
const colorModeButton = document.getElementById("colorModeToggle");
colorModeButton.addEventListener("click", () => {
	colorModeIndex = (colorModeIndex + 1) % colorModes.length;
	colorModeButton.textContent = `Couleurs : ${colorModes[colorModeIndex].name}`;
	applyColorMode(colorModeIndex);
});
function renderFrame() {
	if (isRotating) {
		mesh.rotation.y += 0.003; // rotation lente automatique sur l'axe Y
		mesh.rotation.x += 0.007; 
	}

	// orbites sphériques : chaque luciole a sa vitesse, son rayon et son déphasage
	for (const f of fireflies) {
		f.phase += f.speed;
		const latitude = Math.sin(f.phase * f.latFreq) * f.latAmp;
		const r = Math.cos(latitude) * f.radius;
		f.light.position.set(
			Math.cos(f.phase) * r,
			Math.sin(latitude) * f.radius,
			Math.sin(f.phase) * r
		);
	}

	// décroissance exponentielle du glow : retombe vite au début, puis en douceur
	const glowAttr = geometry.attributes.aGlow;
	for (let i = 0; i < glowAttr.count; i++) {
		const g = glowAttr.getX(i);
		if (g > 0.001) glowAttr.setX(i, g * 0.955); // sinon déjà éteint
	}
	glowAttr.needsUpdate = true;

	// le piston suit le glow de sa face : même easing (sortie au clic, retrait exponentiel)
	for (const piston of pistons) {
		const { basis, center, normal, face } = piston.userData;
		const extension = Math.min(glowAttr.getX(face * 9) / 4, 1) * pistonStroke;
		const origin = center.clone().addScaledVector(normal, extension + pistonRest - starHeight);
		piston.matrix.copy(basis).setPosition(origin);
		piston.matrixWorldNeedsUpdate = true;
	}

	controls.update();
	renderer.render(scene, camera);
	requestAnimationFrame(renderFrame);
}
requestAnimationFrame(renderFrame);

window.addEventListener("resize", () => {
	camera.aspect = window.innerWidth / window.innerHeight;
	camera.updateProjectionMatrix();
	renderer.setSize(window.innerWidth, window.innerHeight);
});