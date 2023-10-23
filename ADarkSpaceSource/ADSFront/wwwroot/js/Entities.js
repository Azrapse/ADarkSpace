import Vector2 from './Vector2.js'

export class Sector {
    id = "";
    name = "No name";
    radius = 2000;
    radiusSquared = 4_000_000;
    time = 0;
    lastUpdateTime = 0;
    nextUpdateTime = 0;
    constructor(radius) {
        this.radius = radius;
        this.radiusSquared = radius * radius;
    }

    updateFromServerData(serverData) {
        this.id = serverData.id;
        this.name = serverData.name;
        this.time = serverData.time;
        this.lastUpdateTime = serverData.lastUpdateTime;
        this.nextUpdateTime = serverData.nextUpdateTime;
    }
}

export class Entity {
    id = "";
    position = new Vector2(0, 0);
    forward = new Vector2(1, 0);
    oldPosition = new Vector2(0, 0);
    oldForward = new Vector2(1, 0);

    get direction() {
        return Math.atan2(this.forward.y, this.forward.x);
    }
    set direction(value) {
        this.forward = new Vector2(Math.cos(value), Math.sin(value));
    }
    speed = 0;
    type;
    sector;

    _sprite;
    _radius;
    _gameobject;

    constructor(type, position, forward, speed, sector) {
        this.type = type;
        this.position = position;
        this.forward = forward;
        this.speed = speed;
        this.sector = sector;
    }

    get velocity() {
        return this.forward.scale(this.speed);
    }

    get isInsideSector() {
        return this.position.magnitudeSqr <= this.sector.radiusSquared;
    }

    update(dt) {
        this.position.add(this.velocity.scale(dt));
    }
}


export class Ship extends Entity {
    name = "No name";
    thinkPeriod = 1;
    thinkElapsed = 0;
    swervingRotationSpeed = 2 * Math.sign(Math.random() - 0.5);

    nextMove = {
        startPosition: new Vector2(0, 0),
        startForward: new Vector2(1, 0),
        endForward: new Vector2(1, 0),
        endPosition: new Vector2(0, 0),
        speed: 1,
        startTime: -1,
        endTime: -1,
        get isMoving() { return (this.speed > 0) && (this.endTime - this.startTime) > 0 && (this.startTime >= 0) && (this.endTime >= 0); },        
    }

    constructor(type, position, forward, speed, sector) {
        super(type, position, forward, speed, sector);
    }

    get sprite() {        
        if (!this._sprite) {
            this._sprite = PIXI.Sprite.from(this.type.img);
            this._sprite.anchor.set(0.5);
            this._sprite.rotation -= this.type.imgdir;
            this._sprite.x = this._sprite.width / 2;
            this._sprite.y = this._sprite.height / 2;
        }
        return this._sprite;
    }
    get radius() {
        if (!this._radius) {
            this._radius = Math.max(this.sprite.width, this.sprite.height) * 0.75;
        }                
        return this._radius;
    }
    get gameobject() {
        if (!this._gameobject) {
            this._gameobject = new PIXI.Container();            
            this._gameobject.addChild(this.sprite);

            // The rectangle boundary
            const boundary = new PIXI.Graphics()
                .lineStyle(1, 0xFFFFFF, 1)
                .drawRect(0, 0, this.sprite.width, this.sprite.height);
            this._gameobject.addChild(boundary);

            // The pivot point
            this._gameobject.pivot.x = this._gameobject.width / 2;
            this._gameobject.pivot.y = this._gameobject.height / 2;
            this._gameobject.x = this.position.x + this.sector.radius;
            this._gameobject.y = this.position.y + this.sector.radius;

            // The forward-facing firing arc
            const firingArc = new PIXI.Graphics()
                .lineStyle(3, 0xFF0000, 1)
                .arc(this._gameobject.pivot.x, this._gameobject.pivot.y, 900, -Math.PI / 4, Math.PI / 4)
                .lineTo(this._gameobject.pivot.x, this._gameobject.pivot.y)
                .closePath()
                .arc(this._gameobject.pivot.x, this._gameobject.pivot.y, 600, -Math.PI / 4, Math.PI / 4)
                .lineTo(this._gameobject.pivot.x, this._gameobject.pivot.y)
                .closePath()
                .arc(this._gameobject.pivot.x, this._gameobject.pivot.y, 300, -Math.PI / 4, Math.PI / 4)
                .lineTo(this._gameobject.pivot.x, this._gameobject.pivot.y)
                .closePath();
            this._gameobject.addChild(firingArc);
            this._gameobject.firingArc = firingArc;
            this._gameobject.firingArc.visible = false;

            // Events
            this._gameobject.eventMode = 'static';
            this._gameobject
                .on("pointerover", () => {
                    this._gameobject.firingArc.visible = true;
                })
                .on("pointerleave", () => {
                    this._gameobject.firingArc.visible = false;
                });
        }
        return this._gameobject;
    }
    setNextMove(startPosition, startForward, speed, endPosition, endForward, startTime, endTime) {
        this.nextMove.startPosition = startPosition;
        this.nextMove.startForward = startForward;
        this.nextMove.endPosition = endPosition;
        this.nextMove.endForward = endForward;
        this.nextMove.speed = speed;
        this.nextMove.startTime = startTime;
        this.nextMove.endTime = endTime;
    }

    interpolateMovement(elapsedTimeSinceMovementStart) {
        if (!this.nextMove.isMoving) {
            return;
        }
        const totalMovementTime = this.nextMove.endTime - this.nextMove.startTime;
        // Calculate the interpolation factor t
        const t = Math.max(0, Math.min(1, elapsedTimeSinceMovementStart / totalMovementTime));

        // Normalize the forward and targetForward vectors
        const normalizedStartForward = this.nextMove.startForward.normalize();
        const normalizedEndForward = this.nextMove.endForward.normalize();

        // Calculate the angle between the initial and final directions
        const directionChange = Math.acos(normalizedStartForward.dot(normalizedEndForward)) * Math.sign(-normalizedStartForward.cross(normalizedEndForward));

        let interpolatedState;

        // If there is no direction change, then the forward direction is constant, and the position is linear.
        if (Math.abs(directionChange < 0.00001)) {
            interpolatedState = {
                position: this.nextMove.startPosition.add(normalizedStartForward.scale(this.speed * t)),
                forward: normalizedStartForward
            };
            return interpolatedState;
        } else {
            // If there is direction change, then the forward direction is interpolated, and the position is circular.
            // Calculate the interpolated position
            const partialDirectionChange = directionChange * t;
            const radius = this.speed / directionChange;

            // Translate the vectorToMove a distance of radius, depending on whether we are turning right or left.
            const originalVector = normalizedStartForward;
            let translationVector = originalVector.perpendicularClockwise * radius;

            const forwardRotatedAroundRadius = this.#translateRotateTranslate(originalVector, translationVector, partialDirectionChange);
            const partialPosition = this.nextMove.startPosition.add(forwardRotatedAroundRadius);

            // Calculate the interpolated forward direction
            const interpolatedForward = normalizedStartForward.rotate((directionChange) * t);

            // Create an object to store the interpolated state
            const interpolatedState = {
                position: partialPosition,
                forward: interpolatedForward
            };

            return interpolatedState;
        }
    }

    #translateRotateTranslate(vector, translation, angle) {
        // Translate the vector
        const translatedVector = vector.add(translation);

        // Rotate the translated vector
        const rotatedVector = translatedVector.rotate(angle);

        // Translate the rotated vector back
        const finalVector = rotatedVector.subtract(translation);

        return finalVector;
    }
    
}
