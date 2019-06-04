'use strict';

let assert = require('assert');

class ReadError extends Error { }
exports.ReadError = ReadError;

function parseSubtypes(type) {
    let subtypeString = type.split('`')[1];
    let subtypeNum = subtypeString.slice(0, 1);

    subtypeString = subtypeString.slice(2, -1);

    let subtypes = [];
    let currentLevel = 0;
    let lastStartingPos = 0;
    for(let i = 0; i < subtypeString.length; i++) {
        let currentChar = subtypeString[i];
        if(currentChar == '[') {
            if(currentLevel == 0) lastStartingPos = i + 1;
            currentLevel += 1;
        } else if(currentChar == ']') {
            currentLevel -= 1;
            if(currentLevel == 0) subtypes.push(subtypeString.slice(lastStartingPos, i));
        }
    }

    assert.equal(subtypeNum, subtypes.length);
    return subtypes;
}

function parseMainType(type) {
    return type.split(/`|,/)[0];
}

function simplifyType(type) {
     let mainType = parseMainType(type);

    let isArray = mainType.endsWith('[]');
    if(isArray) {
        let arrayType = simplifyType(mainType.slice(0, -2));
        return `Array<${arrayType}>`;
    }

    switch(mainType) {
        case 'Microsoft.Xna.Framework.Content.DictionaryReader':
            let subtypes = parseSubtypes(type).map(simplifyType);
            return `Dictionary<${subtypes[0]},${subtypes[1]}>`;

        case 'Microsoft.Xna.Framework.Content.ArrayReader':
            let arrayType = parseSubtypes(type).map(simplifyType)[0];
            return `Array<${arrayType}>`;

        case 'Microsoft.Xna.Framework.Content.ListReader':
            let listType = parseSubtypes(type).map(simplifyType)[0];
            return `List<${listType}>`;

        case 'Microsoft.Xna.Framework.Content.Texture2DReader':
            return 'Texture2D';

        case 'Microsoft.Xna.Framework.Content.Vector3Reader':
        case 'Microsoft.Xna.Framework.Vector3':
            return 'Vector3';

        case 'Microsoft.Xna.Framework.Content.StringReader':
        case 'System.String':
            return 'String';

        case 'Microsoft.Xna.Framework.Content.Int32Reader':
        case 'System.Int32':
            return 'Int32';

        case 'Microsoft.Xna.Framework.Content.CharReader':
        case 'System.Char':
            return 'Char';

        case 'Microsoft.Xna.Framework.Content.BooleanReader':
        case 'System.Boolean':
            return 'Boolean';

        case 'Microsoft.Xna.Framework.Content.SpriteFontReader':
            return 'SpriteFont';

        case 'Microsoft.Xna.Framework.Content.RectangleReader':
        case 'Microsoft.Xna.Framework.Rectangle':
            return 'Rectangle';

        case 'xTile.Pipeline.TideReader':
            return 'TBin';

        default:
            throw new ReadError('Non-implemented type simplifying for "' + type + '"');
    }
}

exports.simplifyType = simplifyType;

function getTypeInfo(type) {
    let mainType = type.match(/[^<]+/)[0];
    let subTypes = type.match(/<(.+)>/);

    if(subTypes) {
        subTypes = subTypes[1].split(',').map(type => type.trim());
    } else {
        subTypes = [];
    }

    return {
        type: mainType,
        subtypes: subTypes
    };
}

exports.getTypeInfo = getTypeInfo;

function isTypeObject(object) {
    return object && object.hasOwnProperty('type') && object.hasOwnProperty('data');
}

exports.isTypeObject = isTypeObject;
