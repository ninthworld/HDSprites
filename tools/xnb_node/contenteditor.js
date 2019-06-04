'use strict';

let util = require('./util');
let fs = require('fs');
let PNG = require('pngjs').PNG;
let path = require('path');
let reader = require('./reader');
let writer = require('./writer');


function objectWalk(object, path, cb) {
    if(!object || typeof object != 'object') return;
    if(!cb) {
        cb = path;
        path = '';
    }

    if(util.isTypeObject(object)) {
        cb(object, path);
        objectWalk(object['data'], path, cb);
    } else {
        if(path) path += '.';
        for(let key in object) {
            objectWalk(object[key], path + key, cb);
        }
    }
}

function onPresave(outputFile, data) {
    let images = [];
    let maps = [];

    objectWalk(data.content, (object, path) => {
        if(object.type == 'Texture2D') images.push(extractImage(object, path, outputFile));
        else if(object.type == 'TBin') maps.push(extractMap(object, path, outputFile));
    });

    if(images.length) data.extractedImages = images;
    if(maps.length) data.extractedMaps = maps;

    return data;
}

exports.onPresave = onPresave;

function onPostload(inputFile, data) {
    let images = data.extractedImages || [];
    for(let i = 0; i < images.length; i++) {
        loadImage(images[i], inputFile, data);
    }

    let maps = data.extractedMaps || [];
    for(let i = 0; i < maps.length; i++) {
        loadMap(maps[i], inputFile, data);
    }

    delete data.extractedImages;
    delete data.extractedMaps;

    return data;
}

exports.onPostload = onPostload;

function traversePath(object, path) {
    if(util.isTypeObject(object)) return traversePath(object['data'], path);
    if(!path) return object;
    let parts = path.split('.');
    return traversePath(object[parts[0]], parts.slice(1).join('.'));
}

function getPathBasename(baseFile, contentPath) {
    let ext = path.extname(baseFile);
    let pathSuffix = contentPath ? '.' + contentPath : '';
    return path.join(path.dirname(baseFile), path.basename(baseFile, ext) + pathSuffix);
}

function extractImage(object, path, outputFile) {
    let png = new PNG({
        width: object.data.width,
        height: object.data.height,
        inputHasAlpha: true
    });

    png.data = object.data.data;

    let filename = getPathBasename(outputFile, path) + '.png';
    let buffer = PNG.sync.write(png);
    fs.writeFileSync(filename, buffer);

    delete object.data.data;
    delete object.data.width;
    delete object.data.height;

    return {
        path: path
    }
}

function loadImage(image, inputFile, data) {
    let filename = getPathBasename(inputFile, image.path) + '.png';
    let pngBuffer = fs.readFileSync(filename);
    let png = PNG.sync.read(pngBuffer);

    let container = traversePath(data.content, image.path);

    container.data = png.data;
    container.width = png.width;
    container.height = png.height;
}

function processMap(data, tilesheetCallback) {
    let buffer = new reader.BufferConsumer(data);
    let header = buffer.consume(6);
    let out = new writer.BufferWriter();
    out.concat(header);

    let skipString = function() {
        let skipBytes = buffer.consume(4).readInt32LE();
        out.writeInt32LE(skipBytes);
        out.concat(buffer.consume(skipBytes));
    };

    let skipProperties = function() {
        let numberProperties = buffer.consume(4).readInt32LE();
        out.writeInt32LE(numberProperties);

        while (numberProperties-- > 0) {
            // skip key string
            skipString();

            let propertyType = buffer.consume(1);
            out.concat(propertyType);
            switch (propertyType[0]) {
                case 0: // bool
                    out.concat(buffer.consume(1));
                    break;
                case 1: // int32
                    out.concat(buffer.consume(4));
                    break;
                case 2: // float (single)
                    out.concat(buffer.consume(4));
                    break;
                case 3: // string
                    skipString();
                    break;
            }
        }
    };

    //skip map id string
    skipString();

    //skip map description string
    skipString();

    skipProperties();

    let numberTileSheets = buffer.consume(4).readInt32LE();
    out.writeInt32LE(numberTileSheets);
    let tileSheets = [];
    while (numberTileSheets-- > 0) {
        skipString(); // skip id
        skipString(); // skip description

        let strBytes = buffer.consume(4).readInt32LE();
        let imgSource = buffer.consume(strBytes);

        imgSource = tilesheetCallback(imgSource);

        out.writeInt32LE(imgSource.length);
        out.writeAscii(imgSource);

        out.concat(buffer.consume(4 * 2 * 4)); // skip sizes, margin and spacing (4 "size" of 2 int32 each)

        skipProperties();
    }

    out.concat(buffer.buffer); // done with tilesets, everything else doesn't matter
    return out.buffer;
}

function extractMap(object, path, outputFile) {
    let tilesheets = [];

    let mapBuffer = processMap(object.data.data, tilesheet => {
        tilesheet += '.png';
        tilesheets.push(tilesheet);
        return tilesheet;
    });

    let mapFile = getPathBasename(outputFile, path) + '.tbin';
    fs.writeFileSync(mapFile, mapBuffer);

    delete object.data.data;

    return {
        path: path,
        tilesheets: tilesheets
    };
}

function loadMap(map, inputFile, data) {
    let mapFile = getPathBasename(inputFile, map.path) + '.tbin';
    let mapBuffer = fs.readFileSync(mapFile);

    mapBuffer = processMap(mapBuffer, tilesheet => {
        return path.basename(tilesheet, '.png');
    });

    let container = traversePath(data.content, map.path);
    container.data = mapBuffer;
}
