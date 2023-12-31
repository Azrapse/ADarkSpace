﻿import Vector2 from './Vector2.js'

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
        turn: 0,
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
            //const boundary = new PIXI.Graphics()
            //    .lineStyle(1, 0xFFFFFF, 1)
            //    .drawRect(0, 0, this.sprite.width, this.sprite.height);
            //this._gameobject.addChild(boundary);

            // The pivot point
            this._gameobject.pivot.x = this._gameobject.width / 2;
            this._gameobject.pivot.y = this._gameobject.height / 2;

            // The position of the gameobject subtracts the Y coord to invert the Y axis, since PIXI.js uses a Y axis that grows downwards
            // but the game uses a Y axis that grows upwards
            this._gameobject.x = this.sector.radius + this.position.x;
            this._gameobject.y = this.sector.radius - this.position.y;

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

    setNextMove(startPosition, startForward, speed, endPosition, endForward, turn, startTime, endTime) {
        this.nextMove.startPosition = startPosition;
        this.nextMove.startForward = startForward;
        this.nextMove.endPosition = endPosition;
        this.nextMove.endForward = endForward;
        this.nextMove.turn = turn;
        this.nextMove.speed = speed;
        this.nextMove.startTime = startTime;
        this.nextMove.endTime = endTime;
    }

    interpolateMovement(elapsedTimeSinceMovementStart) {
        if (!this.nextMove.isMoving) {
            return;
        }
        // Times are provided by the server in milliseconds
        const totalMovementTime = (this.nextMove.endTime - this.nextMove.startTime) / 1000;
        // Calculate the interpolation factor t
        const t = Math.max(0, Math.min(1, elapsedTimeSinceMovementStart / 1000 / totalMovementTime));

        // Normalize the forward and targetForward vectors
        const normalizedStartForward = this.nextMove.startForward.normalize();
        const normalizedEndForward = this.nextMove.endForward.normalize();

        // Calculate the angle between the initial and final directions
        const directionChange = this.nextMove.turn;

        let interpolatedState;

        // If there is no direction change, then the forward direction is constant, and the position is linear.
        if (Math.abs(directionChange) < 0.00001) {
            interpolatedState = {
                position: this.nextMove.startPosition.add(normalizedStartForward.scale(this.nextMove.speed * totalMovementTime * t)),
                forward: normalizedStartForward
            };
            return interpolatedState;
        } else {
            // If there is direction change, then the forward direction is interpolated, and the position is circular.
            // Calculate the interpolated position

            // The key idea here is that to determine the final position after the turn we can just:
            // 1. Determine the radius of the turn. The radius is the length of the arc (the speed times the total time) divided by the angle of the turn.
            const partialDirectionChange = directionChange * t;
            const radius = this.nextMove.speed * totalMovementTime / directionChange;

            // 2. Translate the point to move a distance of radius, depending on whether we are turning right or left.
            // If we are turning right, the translation vector is the left perpendicular of the forward vector, scaled by the radius.
            // If we are turning left, the translation vector is the right perpendicular of the forward vector, scaled by the radius.
            // Because the radius calculation comes out negative when turning right, and positive when turning left, we can just use the right 
            // perpendicular to the forward vector and multiply by the radius.
            const originalPosition = new Vector2(0, 0);
            let translationVector = normalizedStartForward.perpendicularClockwise.scale(radius);
            const forwardRotatedAroundRadius = this.#translateRotateTranslate(originalPosition, translationVector, partialDirectionChange);
            const partialPosition = this.nextMove.startPosition.add(forwardRotatedAroundRadius);

            // Calculate the interpolated forward direction
            const interpolatedForward = normalizedStartForward.rotate(partialDirectionChange);

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

    update(elapsedTimeSinceLastUpdate, originPoint) {
        // Update the ship's position and rotation
        const interpolatedState = this.interpolateMovement(elapsedTimeSinceLastUpdate);
        this.position = interpolatedState.position;
        this.forward = interpolatedState.forward;

        // Update the gameobject
        // The game entities use a coord system where the y axis grow upwards, while PIXI uses a coord system where the y axis grows downwards.
        // Also, rotations are counterclockwise in the game, while they are clockwise in PIXI.
        // So we need to do some conversions.
        this.gameobject.rotation = -this.direction;
        this.gameobject.x = originPoint.x + this.position.x;
        this.gameobject.y = originPoint.y - this.position.y;

    }
}

export class Attack {
    id = "";
    attacker;
    defender;
    damage = 0;
    amount = 0;
    startTime = -1;
    endTime = -1;
    weaponType = {};
    result = "";
    _lifetime = 0;
    _startShootingAt = 1600;
    _endShootingAt = 2000;
    _duration;
    _gameobject;
    _sprite;
    _shotCount;
    _currentProjectileIndex = 0;
    _currentProjectileLifetime = 0;

    constructor(attackData) {
        this.id = attackData.id;
        this.attacker = attackData.attacker;
        this.defender = attackData.defender;
        this.damage = attackData.damage;
        this.amount = attackData.amount;
        this.startTime = attackData.startTime;
        this.endTime = attackData.endTime;
        this.weaponType = attackData.weaponType;
        this.result = attackData.result;
        this._duration = this.endTime - this.startTime;
        this._lifetime = 0;        
        this.gameobject.visible = false;
    }

    get isAlive() {
        return this._lifetime < this._endShootingAt && this._currentProjectileIndex < this.amount;
    }

    get sprite() {
        if (!this._sprite) {
            this._sprite = new PIXI.Sprite(PIXI.Texture.from(this.weaponType.img));
            this._sprite.anchor.set(0.5);
            this._sprite.scale.set(0.5);
            this._sprite.rotation -= this.weaponType.imgdir;
            this._sprite.visible = false;
        }
        return this._sprite;
    }

    get gameobject() {
        if (!this._gameobject) {
            this._gameobject = new PIXI.ParticleContainer(1, {
                scale: true,
                position: true,
                rotation: true,
                uvs: true,
                alpha: true,
            });
            this._gameobject.addChild(this.sprite);
            this.sprite.visible = false;
        }        
        return this._gameobject;
    }

    update(deltaTime, parentPivot) {
        this._lifetime += deltaTime;
        if (!this.isAlive) {
            this.gameobject.visible = false;
            return;
        }
        // Do not draw attacks until the second half of the turn.
        if (this._lifetime < this._startShootingAt) {
            this.gameobject.visible = false;
            return;
        }
        this.gameobject.visible = true;
        const shootingPeriod = this._endShootingAt - this._startShootingAt;
        const individualProjectilePeriod = shootingPeriod / this.amount;

        if (this._currentProjectileLifetime > individualProjectilePeriod) {
            this._currentProjectileLifetime = 0;
            this._currentProjectileIndex++;
        }
        // TODO: Fix so that this is more game-friendly. Currently it requires the user to interact with the webpage, and it plays only one track of it.
        //if (this._currentProjectileLifetime === 0) {
        //    this.weaponType.sfx.play();
        //}        
        this._currentProjectileLifetime += deltaTime;

        const attackerPosition = this.attacker.position.add(this.attacker.forward.scale(30));
        const defenderPosition = this.defender.position;
        const progress = this._currentProjectileLifetime / individualProjectilePeriod;

        const midwayPoint = Vector2.lerp(attackerPosition, defenderPosition, progress);
        const position = new Vector2(parentPivot.x + midwayPoint.x, parentPivot.y - midwayPoint.y);        
        const direction = defenderPosition.subtract(attackerPosition).normalize().angle();
                
        this.sprite.position.set(position.x, position.y);
        this.sprite.rotation = -direction;
        this.sprite.visible = true;        
    }
}
