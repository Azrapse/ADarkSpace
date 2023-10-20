import { Vector2 } from './Vector2.js';

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
/**
 * A dictionary of ship types by id
 */
const shipTypesDict = {};
for (const shipType of shipTypes) {
    shipTypesDict[shipType.name] = shipType;
}

function createSector() {
    const sectorRadius = 2000;
    const container = new PIXI.Container();
    const graphics = new PIXI.Graphics()
        .lineStyle(5, 0xFFF8, 1)
        .beginFill(0x0008)
        .drawCircle(sectorRadius, sectorRadius, sectorRadius)
        .endFill();
    container.addChild(graphics);
    container.pivot.x = container.width / 2;
    container.pivot.y = container.height / 2;
    container.x = app.screen.width / 2;
    container.y = app.screen.height / 2;
    app.stage.addChild(container);

    container.eventMode = 'static';
    container
        .on("wheelcapture", (event) => {
            const globalCoords = event.data.global.clone();
            const localCoords = container.toLocal(globalCoords);

            const scaleFactor = 1 - Math.sign(event.deltaY) * 0.1;
            const newScale = Math.max(0.2, container.scale.x * scaleFactor);
            container.scale.x = newScale;
            container.scale.y = newScale;

            container.x = globalCoords.x + (container.pivot.x - localCoords.x) * newScale;
            container.y = globalCoords.y + (container.pivot.y - localCoords.y) * newScale;
        })
        .on("pointerdown", onDragStart)
        .on("pointerup", onDragEnd)
        .on("pointerupoutside", onDragEnd);

    let dragStartPoint;
    let originalPosition;
    function onDragStart(event) {
        dragStartPoint = event.data.global.clone();
       originalPosition = container.position.clone();
        container.on("pointermove", onDragMove);
    }
    function onDragEnd(event) {
        container.off("pointermove", onDragMove);
    }
    function onDragMove(event) {
        const currentPoint = event.data.global.clone();
        container.position.x = originalPosition.x + currentPoint.x - dragStartPoint.x;
        container.position.y = originalPosition.y + currentPoint.y - dragStartPoint.y;
    }

    const sector = {
        gameobject: container,
        radius: sectorRadius,        
    };
    return sector;
}

/**
 * The ships that are currently in the game
 */
const ships = [];
const shipDict = {};
let sector;

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

/**
 * Give JSON data for one ship got from the ShipUpdater worker container, and an existing ship object from the client, 
 * decides whether to create a new ship object or update it with data from the worker.
 * @param {any} shipData 
 * @param {any} existingShip
 * @returns
 */
function hydrateShip(shipData, existingShip) {
    // Note down the ship type indicated in the data from the worker
    const shipType = shipTypesDict[shipData.shipType];
    let ship;
    // If any local ship was provided, update it with the ship data
    if (existingShip) {
        ship = existingShip;
        ship.name = shipData.name;
        ship.type = shipType;
        ship.direction = shipData.rotation;
        ship.speed = shipData.speed;
        ship.rotationSpeed = shipData.rotationSpeed;
        ship.position = new Vector2(shipData.positionX, shipData.positionY);
    } else {
        // Otherwise, create a new local ship from the ship data and add it to the scene.
        ship = {
            id: shipData.id,
            name: shipData.name,
            type: shipType,
            direction: shipData.rotation,
            speed: shipData.speed,
            rotationSpeed: shipData.rotationSpeed,
            position: new Vector2(shipData.positionX, shipData.positionY),
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
                this.gameobject.x = this.position.x + sector.radius;
                this.gameobject.y = this.position.y + sector.radius;
                return this.gameobject;
            },            
            thinkPeriod: 1,
            thinkElapsed: 0,
            swervingRotationSpeed: 2 * Math.sign(Math.random() - 0.5),
        };
    }
    return ship;
}

/**
 * Poll the worker to obtain up-to-date data about the ships in the game sector
 */
async function synchShips() {
    try {
        // Poll the worker
        const response = await fetch('/PollWorker', { cache: 'no-cache' });
        if (response.ok) {
            const jsonResponse = await response.json();
            let idsFetched = {};
            const serverData = JSON.parse(jsonResponse);
            const sectorData = serverData.sector;
            serverData.ships
                .forEach(shipData => {
                    idsFetched[shipData.id] = true;
                    const oldShip = shipDict[shipData.id];
                    const updatedShip = hydrateShip(shipData, oldShip);
                    if (!oldShip) {
                        ships.push(updatedShip);
                        shipDict[shipData.id] = updatedShip;
                        sector.gameobject.addChild(updatedShip.gameobject);
                    }
                });
            ships.forEach(ship => {
                if (!idsFetched[ship.id]) {
                    sector.gameobject.removeChild(ship.gameobject);
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
async function start() {
    app = new PIXI.Application({ background: '#000010', resizeTo: window });
    const body = document.querySelector("body");
    body.innerHTML="";
    body.appendChild(app.view);

    sector = createSector();

    await synchShips();
    // Add a ticker callback to move the sprites
    app.ticker.add(frameUpdate);

    pollerHandle = setInterval(async () => {
        await synchShips();
    }, 2000);
}

function frameUpdate() {    
    const dt = app.ticker.deltaMS * 0.001;
    for (const ship of ships) {

        ship.direction += ship.rotationSpeed * dt;
        ship.position.x += Math.cos(ship.direction) * ship.speed * dt;
        ship.position.y += Math.sin(ship.direction) * ship.speed * dt;
        ship.gameobject.transform.rotation = ship.direction;
        ship.gameobject.x = ship.position.x + ship.gameobject.parent.pivot.x;
        ship.gameobject.y = ship.position.y + ship.gameobject.parent.pivot.y;

        if (ship.gameobject.rotation < 0) {
            ship.gameobject.rotation += 2 * Math.PI;
        }
        if (ship.gameobject.rotation >= 2 * Math.PI) {
            ship.gameobject.rotation -= 2 * Math.PI;
        }

        if (ship.thinkElapsed >= ship.thinkPeriod) {
            ship.thinkElapsed = 0;

            const lookAhead = {
                x: ship.position.x + Math.cos(ship.direction) * ship.speed * 3,
                y: ship.position.y + Math.sin(ship.direction) * ship.speed * 3
            };

            if (Math.sqrt(lookAhead.x * lookAhead.x + lookAhead.y * lookAhead.y) > sector.radius) {
                ship.rotationSpeed = ship.swervingRotationSpeed;
            } else {
                ship.rotationSpeed = Math.random() * 2 - 1;
            }
        } else {
            ship.thinkElapsed += dt;
        }
    }
}


function end() {
    clearInterval(pollerHandle);
}

export { start, shipTypes, name };