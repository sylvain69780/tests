const game = document.getElementById("game");
const ctx = game.getContext("2d");

const cubeVertices = [
	[-1, -1, -1], [1, -1, -1], [1, 1, -1], [-1, 1, -1],
	[-1, -1, 1], [1, -1, 1], [1, 1, 1], [-1, 1, 1]
];
const cubeEdges = [
	[0, 1], [1, 2], [2, 3], [3, 0],
	[4, 5], [5, 6], [6, 7], [7, 4],
	[0, 4], [1, 5], [2, 6], [3, 7]
];

function renderFrame(timestamp) {
	const angle = timestamp * 0.001;
	const cosineY = Math.cos(angle);
	const sineY = Math.sin(angle);
	const cosineX = Math.cos(angle * 0.7);
	const sineX = Math.sin(angle * 0.7);
	const centerX = game.width / 2;
	const centerY = game.height / 2;
	const scale = 600;
	const cameraDistance = 4;

	ctx.fillStyle = "black";
	ctx.fillRect(0, 0, game.width, game.height);

	const projectedVertices = cubeVertices.map(([x, y, z]) => {
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
	for (const [start, end] of cubeEdges) {
		ctx.moveTo(...projectedVertices[start]);
		ctx.lineTo(...projectedVertices[end]);
	}
	ctx.stroke();

	requestAnimationFrame(renderFrame);
}

requestAnimationFrame(renderFrame);

