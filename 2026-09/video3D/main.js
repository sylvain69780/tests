const game = document.getElementById("game");
const ctx = game.getContext("2d");

const phi = (1 + Math.sqrt(5)) / 2;
const iphi = 1 / phi;
const dodecahedronVertices = [
	[-1, -1, -1], [-1, -1, 1], [-1, 1, -1], [-1, 1, 1],
	[1, -1, -1], [1, -1, 1], [1, 1, -1], [1, 1, 1],
	[0, -iphi, -phi], [0, -iphi, phi],
	[0, iphi, -phi], [0, iphi, phi],
	[-iphi, -phi, 0], [-iphi, phi, 0],
	[iphi, -phi, 0], [iphi, phi, 0],
	[-phi, 0, -iphi], [-phi, 0, iphi],
	[phi, 0, -iphi], [phi, 0, iphi]
];

const dodecahedronEdges = [];
const edgeLengthSquared = 4 / (phi * phi);

for (let start = 0; start < dodecahedronVertices.length; start++) {
	for (let end = start + 1; end < dodecahedronVertices.length; end++) {
		const differenceX = dodecahedronVertices[start][0] - dodecahedronVertices[end][0];
		const differenceY = dodecahedronVertices[start][1] - dodecahedronVertices[end][1];
		const differenceZ = dodecahedronVertices[start][2] - dodecahedronVertices[end][2];
		const distanceSquared = differenceX ** 2 + differenceY ** 2 + differenceZ ** 2;

		if (Math.abs(distanceSquared - edgeLengthSquared) < 1e-9) {
			dodecahedronEdges.push([start, end]);
		}
	}
}

function renderFrame(timestamp) {
	const angle = timestamp * 0.001;
	const cosineY = Math.cos(angle);
	const sineY = Math.sin(angle);
	const cosineX = Math.cos(angle * 0.7);
	const sineX = Math.sin(angle * 0.7);
	const centerX = game.width / 2;
	const centerY = game.height / 2;
	const scale = 250;
	const cameraDistance = 4;

	ctx.fillStyle = "black";
	ctx.fillRect(0, 0, game.width, game.height);

	const projectedVertices = dodecahedronVertices.map(([x, y, z]) => {
		const rotatedX = x * cosineY - z * sineY;
		const rotatedZ = x * sineY + z * cosineY;
		const tiltedY = y * cosineX - rotatedZ * sineX;
		const tiltedZ = y * sineX + rotatedZ * cosineX;
		const perspective = scale / (cameraDistance + tiltedZ);

		return [
			centerX + rotatedX * perspective,
			centerY + tiltedY * perspective
		];
	});

	ctx.strokeStyle = "#55d6be";
	ctx.lineWidth = 3;
	ctx.beginPath();
	for (const [start, end] of dodecahedronEdges) {
		ctx.moveTo(...projectedVertices[start]);
		ctx.lineTo(...projectedVertices[end]);
	}
	ctx.stroke();

	requestAnimationFrame(renderFrame);
}

requestAnimationFrame(renderFrame);

