import { Vector2 } from './Vector2.js';
import { Sector, Entity, Ship, Attack } from './Entities.js';

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

const weaponTypes = [
    { name: "Red Laser", img: 'img/red laser.png', imgdir: 0, speed: 100, sfx: new Audio('sfx/green laser.mp3') },
    { name: "Green Laser", img: 'img/red laser.png', imgdir: 0, speed: 100 },
    { name: "Missile", img: 'img/red laser.png', imgdir: 0, speed: 100 },
    { name: "Torpedo", img: 'img/red laser.png', imgdir: 0, speed: 100 },
];

/**
 * A dictionary of ship types by id
 */
const shipTypesDict = {};
for (const shipType of shipTypes) {
    shipTypesDict[shipType.name] = shipType;
}
const weaponTypesDict = {};
for (const weaponType of weaponTypes) {
    weaponTypesDict[weaponType.name] = weaponType;
}

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
 * The ships and attacks that are currently in the game
 */
const ships = [];
const shipDict = {};
const attacks = [];
const attackDict = {};
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

function hydrateAttack(attackData, existingAttack) {
    const weaponType = weaponTypesDict[attackData.weapon];
    let attack;
    if (existingAttack) {
        attack = existingAttack;
        attack.id = attackData.id;
        attack.sectorId = attackData.sectorId;
        attack.weaponType = weaponType;
        attack.attacker = shipDict[attackData.attackerId];
        attack.defender = shipDict[attackData.defenderId];
        attack.startTime = attackData.startTime;
        attack.endTime = attackData.endTime;
        attack.amount = attackData.amount;
        attack.damage = attackData.damage;
        attack.result = attackData.result;
    } else {
        attack = new Attack({
            id: attackData.id,
            sectorId: attackData.sectorId,
            weaponType: weaponType,
            attacker: shipDict[attackData.attackerId],
            defender: shipDict[attackData.defenderId],
            startTime: attackData.startTime,
            endTime: attackData.endTime,
            amount: attackData.amount,
            damage: attackData.damage,
            result: attackData.result
        });
    }
    return attack;
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
            synchEntities(serverData, idsFetched);

            elapsedTimeSinceLastUpdate = 0;
        }
    }
    catch (error) {
        console.log(error);
    }        
}
function synchEntities(serverData, idsFetched) {
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

    // Update the attacks
    serverData.attacks
        .forEach(attackData => {
            // Note down the attack id as fetched from the worker
            idsFetched[attackData.id] = true;
            // Get the attack object from the local dictionary
            const oldAttack = attackDict[attackData.id];
            // Update the attack object with the data from the worker
            const updatedAttack = hydrateAttack(attackData, oldAttack);
            // If the attack was not in the local dictionary, add it to the scene
            if (!oldAttack) {
                attacks.push(updatedAttack);
                attackDict[attackData.id] = updatedAttack;
                sector.gameobject.addChild(updatedAttack.gameobject);
            }
        });
    // Remove attacks that were not fetched from the worker.
    attacks.forEach(attack => {
        if (!idsFetched[attack.id]) {
            attack.sprite.visible = false;          
            sector.gameobject.removeChild(attack.gameobject);
            attacks.splice(attacks.indexOf(attack), 1);
            delete attackDict[attack.id];
        }
    });
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
    const originPoint = new Vector2(sector.gameobject.pivot.x, sector.gameobject.pivot.y);
    // Update the position of the ships
    for (const ship of ships) {
        ship.update(elapsedTimeSinceLastUpdate, originPoint);
    }

    // Update the attacks
    for (const attack of attacks) {
        attack.update(dt, originPoint);
    }
}

function end() {
    clearInterval(pollerHandle);
}

export { start, end, shipTypes, name };