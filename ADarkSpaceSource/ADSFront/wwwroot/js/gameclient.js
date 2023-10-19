// <reference path="../lib/pixi.js/pixi.js" />
const name = "gameclient";

/**
 * The core PIXI app object.
 */
let app;

/**
 * The types of ships that can be spawned
 */
const shipTypes = [
    { name: "X-Wing", img: 'img/x-wing.png', imgdir: Math.PI, speed: 100 },
    { name: "A-Wing", img: 'img/a-wing.png', imgdir: Math.PI, speed: 120 },
    { name: "Y-Wing", img: 'img/y-wing.png', imgdir: Math.PI, speed: 80 },
    { name: "B-Wing", img: 'img/b-wing.png', imgdir: Math.PI, speed: 90 },
    { name: "TIE Fighter", img: 'img/t-f.png', imgdir: Math.PI, speed: 100 },
    { name: "TIE Interceptor", img: 'img/t-i.png', imgdir: Math.PI, speed: 115 },
    { name: "TIE Bomber", img: 'img/t-b.png', imgdir: Math.PI, speed: 80 },
    { name: "TIE Advanced", img: 'img/t-a.png', imgdir: Math.PI, speed: 120 },
    { name: "TIE Defender", img: 'img/t-d.png', imgdir: Math.PI, speed: 120 },
    { name: "Shuttle", img: 'img/shuttle.png', imgdir: Math.PI, speed: 60 },
    { name: "Starwing", img: 'img/starwing.png', imgdir: Math.PI, speed: 90 },
];

const shipTypesDict = {};
for (const shipType of shipTypes) {
    shipTypesDict[shipType.name] = shipType;
}

const ships = [];
const shipDict = {};
let pollerHandle;
function spawnShips() {
    for (let i = 0; i < 200; i++) {
        const pick = shipTypes[Math.floor(Math.random() * shipTypes.length)];
        const ship = Object.assign({}, pick);
        ships.push(ship);
    }
    for (const element of ships) {
        let shipType = element;
        shipType.direction = 0;
        let sprite = PIXI.Sprite.from(shipType.img);
        sprite.anchor.set(0.5);
        sprite.rotation -= shipType.imgdir;
        shipType.radius = Math.max(sprite.width, sprite.height) * 0.75;

        const gameobject = new PIXI.Container();
        shipType.gameobject = gameobject;
        gameobject.addChild(sprite);
        gameobject.pivot.x = gameobject.width / 2;
        gameobject.pivot.y = gameobject.height / 2;

        shipType.rotationSpeed = Math.random() * .2 - .1;

        // Create the sprite and add it to the stage
        shipType.direction = Math.random() * Math.PI * 2;
        gameobject.x = (Math.random() + 1) * app.screen.width / 2;
        gameobject.y = (Math.random() + 1) * app.screen.height / 2;

        shipType.thinkPeriod = 1;
        shipType.thinkElapsed = 0;
        shipType.swervingRotationSpeed = 2 * Math.sign(Math.random() - 0.5);

        app.stage.addChild(gameobject);
    }
}

function hydrateShip(shipData, existingShip) {
    const shipType = shipTypesDict[shipData.shipType];
    let ship;
    if (existingShip) {
        ship = existingShip;
        ship.name = shipData.name;
        ship.type = shipType;
        ship.direction = shipData.rotation;
        ship.speed = shipData.speed;
        ship.rotationSpeed = shipData.rotationSpeed;
        ship.position.x = shipData.positionX;
        ship.position.y = shipData.positionY;
    } else {
        ship = {
            id: shipData.id,
            name: shipData.name,
            type: shipType,
            direction: shipData.rotation,
            speed: shipData.speed,
            rotationSpeed: shipData.rotationSpeed,
            get sprite() {
                delete this.sprite;
                this.sprite = PIXI.Sprite.from(shipType.img);
                this.sprite.anchor.set(0.5);
                this.sprite.rotation -= shipType.imgdir;
                return this.sprite;
            },
            get radius() {
                delete this.radius;
                this.radius = Math.max(this.sprite.width, this.sprite.height) * 0.75;
                return this.radius;
            },
            get gameobject() {
                delete this.gameobject;
                this.gameobject = new PIXI.Container();
                this.gameobject.addChild(this.sprite);
                this.gameobject.pivot.x = this.gameobject.width / 2;
                this.gameobject.pivot.y = this.gameobject.height / 2;
                this.gameobject.x = shipData.positionX;
                this.gameobject.y = shipData.positionY;
                return this.gameobject;
            },
            get position() {
                const ship = this;
                return {
                    get x() { return ship.gameobject.x },
                    set x(value) { ship.gameobject.x = value },
                    get y() { return ship.gameobject.y },
                    set y(value) { ship.gameobject.y = value }
                };
            },
            set position(value) {
                this.gameobject.x = value.x;
                this.gameobject.y = value.y;
            },
            thinkPeriod: 1,
            thinkElapsed: 0,
            swervingRotationSpeed: 2 * Math.sign(Math.random() - 0.5),
        };
    }
    return ship;
}

async function synchShips() {
    try {
        const response = await fetch('/PollWorker', { cache: 'no-cache' });
        if (response.ok) {
            const jsonResponse = await response.json();
            let idsFetched = {};
            JSON.parse(jsonResponse)
                .forEach(shipData => {
                    idsFetched[shipData.id] = true;
                    const oldShip = shipDict[shipData.id];
                    const updatedShip = hydrateShip(shipData, oldShip);
                    if (!oldShip) {
                        ships.push(updatedShip);
                        shipDict[shipData.id] = updatedShip;
                        app.stage.addChild(updatedShip.gameobject);
                    }
                });
            ships.forEach(ship => {
                if (!idsFetched[ship.id]) {
                    app.stage.removeChild(ship.gameobject);
                    ships.splice(ships.indexOf(ship), 1);
                    delete shipDict[ship.id];
                }
            });
        }
    }
    catch (error) {
        console.log(error);
    }        
}


/**
 * Starts the game
 */
function start() {
    app = new PIXI.Application({ background: '#000010', resizeTo: window });
    document.querySelector("body").appendChild(app.view);

    //spawnShips();

    // Add a ticker callback to move the sprites
    app.ticker.add(() => {
        const dt = app.ticker.deltaMS * 0.001;
        for (const ship of ships) {

            ship.direction += ship.rotationSpeed * dt;
            ship.gameobject.transform.rotation = ship.direction;
            ship.gameobject.x += Math.cos(ship.direction) * ship.speed * dt;
            ship.gameobject.y += Math.sin(ship.direction) * ship.speed * dt;

            ship.gameobject.x = Math.max(ship.radius, Math.min(app.screen.width - ship.radius, ship.gameobject.x));
            ship.gameobject.y = Math.max(ship.radius, Math.min(app.screen.height - ship.radius, ship.gameobject.y));
            if (ship.gameobject.rotation < 0) {
                ship.gameobject.rotation += 2 * Math.PI;
            }
            if (ship.gameobject.rotation >= 2 * Math.PI) {
                ship.gameobject.rotation -= 2 * Math.PI;
            }

            if (ship.thinkElapsed >= ship.thinkPeriod) {
                ship.thinkElapsed = 0;

                const lookAhead = {
                    x: ship.gameobject.x + Math.cos(ship.direction) * ship.speed * 3,
                    y: ship.gameobject.y + Math.sin(ship.direction) * ship.speed * 3
                };
                if (lookAhead.x < 0 || lookAhead.x > app.screen.width || lookAhead.y < 0 || lookAhead.y > app.screen.height) {
                    ship.rotationSpeed = ship.swervingRotationSpeed;
                } else {
                    ship.rotationSpeed = Math.random() * 2 - 1;
                }
            } else {
                ship.thinkElapsed += dt;
            }
        }
    });

    pollerHandle = setInterval(async () => {
        console.clear();
        console.log("Polling");
        await synchShips();
    }, 2000);
}

function end() {
    clearInterval(pollerHandle);
}

export { start, shipTypes, name };