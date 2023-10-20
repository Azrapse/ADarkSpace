/**
 * A 2D vector class. 
 * Modified from source: https://gist.github.com/qazd/dadf59318ea49017a8b74ac7a0a49d99
 */
class Vector2 {
	x = 0;
	y = 0;

	constructor(x, y) {
		this.set(x, y);
	}

	set(x, y) {
		this.x = x || 0;
		this.y = y || 0;
	}

	clone() {
		return new Vector2(this.x, this.y)
	}

	add(vector) {
		return new Vector2(this.x + vector.x, this.y + vector.y);
	}

	subtract(vector) {
		return new Vector2(this.x - vector.x, this.y - vector.y);
	}

	scale(scalar) {
		return new Vector2(this.x * scalar, this.y * scalar);
	}

	dot(vector) {
		return (this.x * vector.x + this.y * vector.y);
	}

	moveTowards(vector, t) {
		// Linearly interpolates between vectors A and B by t.
		// t = 0 returns A, t = 1 returns B
		t = Math.min(t, 1); // still allow negative t
		let diff = vector.subtract(this);
		return this.add(diff.scale(t));
	}

	get magnitude() {
		return Math.sqrt(this.magnitudeSqr());
	}

	get magnitudeSqr() {
		return (this.x * this.x + this.y * this.y);
	}

	distance(vector) {
		return Math.sqrt(this.distanceSqr(vector));
	}

	distanceSqr(vector) {
		let deltaX = this.x - vector.x;
		let deltaY = this.y - vector.y;
		return (deltaX * deltaX + deltaY * deltaY);
	}

	normalize() {
		let mag = this.magnitude();
		let vector = this.clone();
		if (Math.abs(mag) < 1e-9) {
			vector.x = 0;
			vector.y = 0;
		} else {
			vector.x /= mag;
			vector.y /= mag;
		}
		return vector;
	}

	angle() {
		return Math.atan2(this.y, this.x);
	}

	angleTo(vector) {
		return Math.atan2(vector.y - this.y, vector.x - this.x);
	}

	rotate(alpha) {
		let cos = Math.cos(alpha);
		let sin = Math.sin(alpha);
		let vector = new Vector2();
		vector.x = this.x * cos - this.y * sin;
		vector.y = this.x * sin + this.y * cos;
		return vector;
	}

	toPrecision(precision) {
		let vector = this.clone();
		vector.x = vector.x.toFixed(precision);
		vector.y = vector.y.toFixed(precision);
		return vector;
	}

	toString() {
		let vector = this.toPrecision(1);
		return (`[${vector.x}; ${vector.y}]`);
	}
};

export { Vector2 };