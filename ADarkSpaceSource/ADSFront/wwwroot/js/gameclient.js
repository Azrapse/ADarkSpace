import { Vector2 } from './Vector2.js';
import { Sector, Entity, Ship } from './Entities.js';

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
    { name: "TIE Bomber", img: 'img/t-b.png', imgdir: Math.PI, speed: 180 },
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

let redDot;
let greenDot;

function createSector() {
    const sectorRadius = 2000;
    const container = new PIXI.Container();
    container.sortableChildren = true;
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

    redDot = new PIXI.Graphics()
        .lineStyle(1, 0xFF0000, 1)
        .beginFill(0xAA0000)
        .drawCircle(container.pivot.x, container.pivot.y, 5)
        .endFill();
    redDot.zIndex = 1000;
    container.addChild(redDot);

    greenDot = new PIXI.Graphics()
        .lineStyle(1, 0x00FF00, 1)
        .beginFill(0x00AA00)
        .drawCircle(container.pivot.x, container.pivot.y, 5)
        .endFill();
    greenDot.zIndex = 1001;

    container.addChild(greenDot);

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

    const sector = new Sector(sectorRadius);
    sector.gameobject = container;
    return sector;
}


/**
 * The ships that are currently in the game
 */
const ships = [];
const shipDict = {};
let sector;

let pollerHandle;

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
        ship.oldPosition = ship.position;
        ship.position = new Vector2(shipData.startPositionX, shipData.startPositionY);
        ship.forward = new Vector2(shipData.startForwardX, shipData.startForwardY);                
    } else {
        // Otherwise, create a new local ship from the ship data and add it to the scene.
        ship = new Ship(
            shipType,
            new Vector2(shipData.startPositionX, shipData.startPositionY),
            new Vector2(shipData.startForwardX, shipData.startForwardY),
            shipData.speed,
            sector
        );
        ship.id = shipData.id;
        ship.name = shipData.name;
        ship.oldPosition = ship.position;
    }
    ship.setNextMove(new Vector2(shipData.startPositionX, shipData.startPositionY),
        new Vector2(shipData.startForwardX, shipData.startForwardY),
        shipData.speed,
        new Vector2(shipData.endPositionX, shipData.endPositionY),
        new Vector2(shipData.endForwardX, shipData.endForwardY),
        shipData.targetTurn,
        shipData.movementStartTime,
        shipData.movementEndTime);
    return ship;
}

/**
 * Poll the worker to obtain up-to-date data about the ships in the game sector
 */
async function synchServerData() {
    try {
        // Poll the worker
        const response = await fetch('/PollWorker', { cache: 'no-cache' });
        // If it answers, process the response
        if (response.ok) {
            const jsonResponse = await response.json();
            const idsFetched = {};
            const serverData = JSON.parse(jsonResponse);

            // Update the sector data
            const lastLocalUpdateTime = sector.lastUpdateTime;
            sector.updateFromServerData(serverData.sector);
            if (sector.lastUpdateTime <= lastLocalUpdateTime) {
                console.log('Sector data is stale. Skipping update.');
                return;
            }            
            // Update the ships
            serverData.ships
                .forEach(shipData => {
                    // Note down the ship id as fetched from the worker
                    idsFetched[shipData.id] = true;
                    // Get the ship object from the local dictionary
                    const oldShip = shipDict[shipData.id];
                    // Update the ship object with the data from the worker
                    const updatedShip = hydrateShip(shipData, oldShip);
                    // If the ship was not in the local dictionary, add it to the scene
                    if (!oldShip) {
                        ships.push(updatedShip);
                        shipDict[shipData.id] = updatedShip;
                        sector.gameobject.addChild(updatedShip.gameobject);
                    }
                });
            // Remove ships that were not fetched from the worker. They must have been destroyed or something.
            ships.forEach(ship => {
                if (!idsFetched[ship.id]) {
                    sector.gameobject.removeChild(ship.gameobject);
                    ships.splice(ships.indexOf(ship), 1);
                    delete shipDict[ship.id];
                }
            });
            elapsedTimeSinceLastUpdate = 0;
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

    await periodicallySynch();
    // Add a ticker callback to move the sprites
    app.ticker.add(frameUpdate);    
}

async function periodicallySynch() {
    await synchServerData();
    pollerHandle = setTimeout(periodicallySynch, 2000);
}

let elapsedTimeSinceLastUpdate = 0;
function frameUpdate() {    
    const dt = app.ticker.deltaMS;
    elapsedTimeSinceLastUpdate += dt;
    const now = Date.now();
    if (now < 0) {
        return;
    }

    for (const ship of ships) {
        // Update the ship's position and rotation
        const interpolatedState = ship.interpolateMovement(elapsedTimeSinceLastUpdate);
        ship.position = interpolatedState.position;
        ship.forward = interpolatedState.forward;

        // Update the gameobject
        // The game entities use a coord system where the y axis grow upwards, while PIXI uses a coord system where the y axis grows downwards.
        // Also, rotations are counterclockwise in the game, while they are clockwise in PIXI.
        // So we need to do some conversions.
        ship.gameobject.rotation = -ship.direction;
        ship.gameobject.x = ship.gameobject.parent.pivot.x + ship.position.x;
        ship.gameobject.y = ship.gameobject.parent.pivot.y - ship.position.y;
    }
    redDot.x = ships[0].nextMove.startPosition.x;
    redDot.y = -ships[0].nextMove.startPosition.y;
    greenDot.x = ships[0].nextMove.endPosition.x;
    greenDot.y = -ships[0].nextMove.endPosition.y;

}


function end() {
    clearInterval(pollerHandle);
}

export { start, end, shipTypes, name };