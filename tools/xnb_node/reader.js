'use strict';

let assert = require('assert');
let util = require('./util');

class BufferConsumer {
    constructor(buffer) {
        this.buffer = buffer;
        this.position = 0;
    }

    get length() {
        return this.buffer.length;
    }

    consume(amount) {
        let slice = this.buffer.slice(0, amount);
        this.buffer = this.buffer.slice(amount);
        this.position += amount;
        return slice;
    }

    consume7BitEncodedNumber() {
        let number = 0;
        let byte = 0;
        let bitsRead = 0;

        do {
            byte = this.consume(1).readUInt8();
            number += (byte & ~128) << bitsRead;
            bitsRead += 7;
        } while(byte & 128);

        return number;
    }
}

exports.BufferConsumer = BufferConsumer;

class Reader {
    consume(buffer, readerResolver) {
        return {
            type: this.type,
            data: this._consume(buffer, readerResolver)
        };
    }

    isValueType() {
        return true;
    }

    get type() {
        throw new Exception('Non-overloaded type method');
    }
}

class DictionaryReader extends Reader {
    constructor(keyReader, valueReader) {
        super();
        this.keyReader = keyReader;
        this.valueReader = valueReader;
    }

    _consume(buffer, readerResolver) {
        let dict = {};

        let count = buffer.consume(4).readUInt32LE();
        for(let i = 0; i < count; i++) {
            let key = this.keyReader.isValueType() ? this.keyReader.consume(buffer, readerResolver) : readerResolver.consume(buffer);
            let value = this.valueReader.isValueType() ? this.valueReader.consume(buffer, readerResolver) : readerResolver.consume(buffer);
            if(key.type) key = key.data;
            dict[key] = value;
        }

        return dict;
    }

    get type() {
        return `Dictionary<${this.keyReader.type},${this.valueReader.type}>`;
    }

    isValueType() {
        return false;
    }
}

class ArrayReader extends Reader {
    constructor(elementReader) {
        super();
        this.elementReader = elementReader;
    }

    _consume(buffer, readerResolver) {
        let array = [];

        let count = buffer.consume(4).readUInt32LE();
        for(let i = 0; i < count; i++) {
            let element = this.elementReader.isValueType() ? this.elementReader.consume(buffer, readerResolver) : readerResolver.consume(buffer);
            array.push(element);
        }

        return array;
    }

    get type() {
        return `Array<${this.elementReader.type}>`;
    }

    isValueType() {
        return false;
    }
}

class ListReader extends ArrayReader {
    get type() {
        return `List<${this.elementReader.type}>`;
    }
}

class Texture2DReader extends Reader {
    _consume(buffer, readerResolver) {
        let format = buffer.consume(4).readInt32LE();
        let width = buffer.consume(4).readUInt32LE();
        let height = buffer.consume(4).readUInt32LE();
        let count = buffer.consume(4).readUInt32LE();
        assert.equal(count, 1);

        let size = buffer.consume(4).readUInt32LE();
        let data = buffer.consume(size);

        let dxt = require('dxt');
        if(format == 4) {
            data = dxt.decompress(data, width, height, dxt.kDxt1);
        } else if(format == 5) {
            data = dxt.decompress(data, width, height, dxt.kDxt3);
        } else if(format == 6) {
            data = dxt.decompress(data, width, height, dxt.kDxt5);
        } else if(format != 0) {
            throw new util.ReadError('Non-implemented Texture2D type: ' + format);
        }

        for(let i = 0; i < data.length; i += 4) {
            let inverseAlpha = 255 / data[i + 3];
            data[i] = Math.ceil(data[i] * inverseAlpha);
            data[i + 1] = Math.ceil(data[i + 1] * inverseAlpha);
            data[i + 2] = Math.ceil(data[i + 2] * inverseAlpha);
        }

        // Uncomment this for testing, as compression changes the buffer each time.
        // format = 0;

        return {
            format,
            width,
            height,
            data
        };
    }

    get type() {
        return 'Texture2D';
    }

    isValueType() {
        return false;
    }
}


class SpriteFontReader extends Reader {
    _consume(buffer, readerResolver) {

        let texture = readerResolver.consume(buffer);
        let glyphs = readerResolver.consume(buffer);
        let cropping = readerResolver.consume(buffer);
        let characterMap = readerResolver.consume(buffer);
        let verticalSpacing = buffer.consume(4).readInt32LE();
        let horizontalSpacing = buffer.consume(4).readFloatLE();
        let kerning = readerResolver.consume(buffer);

        let nullableCharReader = new NullableReader(new CharReader());
        let defaultCharacter = nullableCharReader.consume(buffer);

        return {
            texture,
            glyphs,
            cropping,
            characterMap,
            verticalSpacing,
            horizontalSpacing,
            kerning,
            defaultCharacter
        };
    }

    get type() {
        return 'SpriteFont';
    }

    isValueType() {
        return false;
    }
}

class TBinReader extends Reader {
    _consume(buffer, readerResolver) {

        let size = buffer.consume(4).readInt32LE();

        let data = buffer.consume(size);

        return {
            data: data
        };
    }

    isValueType() {
        return false;
    }

    get type() {
        return 'TBin';
    }
}

class Vector3Reader extends Reader {
    _consume(buffer, readerResolver) {
        return {
            x: buffer.consume(4).readFloatLE(),
            y: buffer.consume(4).readFloatLE(),
            z: buffer.consume(4).readFloatLE(),
        };
    }

    get type() {
        return 'Vector3';
    }
}

function Utf8CharSize(byte) {
    // From http://stackoverflow.com/a/2954379
    return (( 0xE5000000 >> (( byte >> 3 ) & 0x1e )) & 3 ) + 1;
}

class CharReader extends Reader {
    _consume(buffer, readerResolver) {
        let charSize = Utf8CharSize(buffer.buffer[0]);
        return buffer.consume(charSize).toString('utf8');
    }

    get type() {
        return 'Char';
    }
}

class StringReader extends Reader {
    _consume(buffer, readerResolver) {
        let size = buffer.consume7BitEncodedNumber();
        let string = buffer.consume(size).toString('utf8');
        return string;
    }

    get type() {
        return 'String';
    }

    isValueType() {
        return false;
    }
}

exports.StringReader = StringReader;

class NullableReader extends Reader {
    constructor(elementReader) {
        super();
        this.elementReader = elementReader;
    }

    _consume(buffer, readerResolver) {
        let booleanReader = new BooleanReader();
        let hasValue = booleanReader._consume(buffer, readerResolver);
        let value = null;
        if(hasValue) {
            value = this.elementReader.isValueType() ? this.elementReader.consume(buffer, readerResolver) : readerResolver.consume(buffer);
        }

        return {
            data: value
        };
    }
    get type() {
        return `Nullable<${this.elementReader.type}>`
    }
}


class RectangleReader extends Reader {
    _consume(buffer, readerResolver) {
        return {
            x: buffer.consume(4).readInt32LE(),
            y: buffer.consume(4).readInt32LE(),
            width: buffer.consume(4).readInt32LE(),
            height: buffer.consume(4).readInt32LE()
        };
    }

    get type() {
        return 'Rectangle';
    }
}

class Int32Reader extends Reader {
    _consume(buffer, readerResolver) {
        return buffer.consume(4).readInt32LE();
    }

    get type() {
        return 'Int32';
    }
}

class BooleanReader extends Reader {
    _consume(buffer, readerResolver) {
        return Boolean(buffer.consume(1).readUInt8());
    }

    get type() {
        return 'Boolean';
    }
}

class ReaderResolver {
    constructor(readers) {
        this.readers = readers;
    }

    consume(buffer) {
        let index = buffer.consume7BitEncodedNumber() - 1;
        return this.readers[index].consume(buffer, this);
    }
}

exports.ReaderResolver = ReaderResolver;

function getReader(type) {
    let typeInfo = util.getTypeInfo(type);
    typeInfo.subtypes = typeInfo.subtypes.map(getReader);

    switch(typeInfo.type) {
        case 'Dictionary':
            return new DictionaryReader(typeInfo.subtypes[0], typeInfo.subtypes[1]);

        case 'Array':
            return new ArrayReader(typeInfo.subtypes[0]);

        case 'List':
            return new ListReader(typeInfo.subtypes[0]);

        case 'Texture2D':
            return new Texture2DReader();

        case 'Vector3':
            return new Vector3Reader();

        case 'String':
            return new StringReader();

        case 'Int32':
            return new Int32Reader();

        case 'Char':
            return new CharReader();

        case 'Boolean':
            return new BooleanReader();

        case 'SpriteFont':
            return new SpriteFontReader();

        case 'Rectangle':
            return new RectangleReader();

        case 'TBin':
            return new TBinReader();

        default:
            throw new util.ReadError('Non-implemented file reader for "' + type + '"');
    }
}

exports.getReader = getReader;
