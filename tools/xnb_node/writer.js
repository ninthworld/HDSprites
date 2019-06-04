'use strict';

let ref = require('ref');
let util = require('./util');
let reader = require('./reader');
let assert = require('assert');

class BufferWriter {
    constructor(buffer) {
        this.buffer = new Buffer(buffer || 0);
    }

    get length() {
        return this.buffer.length;
    }

    concat(buffer) {
        this.position += buffer.length;
        this.buffer = Buffer.concat([this.buffer, buffer]);
        this.buffer.type = ref.types.byte;
    }

    writeAscii(text) {
        let buffer = new Buffer(text.length);
        buffer.write(text, 0, text.length, 'ascii');
        this.concat(buffer);
    }

    writeByte(number) {
        let buffer = new Buffer(1);
        buffer.writeUInt8(number, 0);
        this.concat(buffer);
    }

    writeInt32LE(number) {
        let buffer = new Buffer(4);
        buffer.writeInt32LE(number, 0);
        this.concat(buffer);
    }

    writeUInt32LE(number) {
        let buffer = new Buffer(4);
        buffer.writeUInt32LE(number, 0);
        this.concat(buffer);
    }

    writeFloatLE(number) {
        let buffer = new Buffer(4);
        buffer.writeFloatLE(number, 0);
        this.concat(buffer);
    }

    write7BitEncodedNumber(number) {
        do {
            let byte = number & 127;
            number = number >> 7;
            if(number) byte |= 128;
            this.writeByte(byte);
        } while(number);
    }
}

exports.BufferWriter = BufferWriter;

class DictionaryWriter {
    constructor(keyType, valueType) {
        this.keyType = keyType;
        this.valueType = valueType;
    }

    write(buffer, dict, writerResolver) {
        let count = Object.keys(dict).length;
        buffer.writeInt32LE(count);
        for(let key of Object.keys(dict)) {
            let value = dict[key];

            // Can't keep track of the key types in javascript without using
            // another storage method.
            writerResolver.write(buffer, {type: this.keyType, data: key});
            writerResolver.write(buffer, value);
        }
    }
}

class ArrayWriter {
    constructor(elementType) {
        this.elementType = elementType;
    }

    write(buffer, array, writerResolver) {
        buffer.writeInt32LE(array.length);
        for(let i = 0; i < array.length; i++) {
            writerResolver.write(buffer, array[i]);
        }
    }
}

class Texture2DWriter {
    write(buffer, imageData, writerResolver) {
        buffer.writeInt32LE(0);
        buffer.writeUInt32LE(imageData.width);
        buffer.writeUInt32LE(imageData.height);
        buffer.writeUInt32LE(1);

        if(!imageData.format) imageData.format = 0;
        if(!imageData.shouldCompress) imageData.format = 0;

        let data = imageData.data;

        for(let i = 0; i < data.length; i += 4) {
            let alpha = data[i + 3] / 255;
            data[i] = Math.floor(data[i] * alpha);
            data[i + 1] = Math.floor(data[i + 1] * alpha);
            data[i + 2] = Math.floor(data[i + 2] * alpha);
        }

        let dxt = require('dxt');
        if(imageData.format == 3) {
            data = dxt.compress(data, width, height, dxt.kDxt1);
        } else if(imageData.format == 4) {
            data = dxt.compress(data, width, height, dxt.kDxt3);
        } else if(imageData.format == 5) {
            data = dxt.compress(data, width, height, dxt.kDxt5);
        }

        buffer.writeUInt32LE(data.length);
        buffer.concat(data);
    }
}

class SpriteFontWriter {
    write(buffer, fontData, writerResolver) {
        writerResolver.write(buffer, fontData.texture);
        writerResolver.write(buffer, fontData.glyphs);
        writerResolver.write(buffer, fontData.cropping);
        writerResolver.write(buffer, fontData.characterMap);
        buffer.writeInt32LE(fontData.verticalSpacing);
        buffer.writeFloatLE(fontData.horizontalSpacing);
        writerResolver.write(buffer, fontData.kerning);

        let defaultCharacter = fontData.defaultCharacter.data;
        let booleanWriter = new BooleanWriter();
        if(defaultCharacter.data != null) {
            booleanWriter.write(buffer, true, writerResolver);

            let charWriter = new CharWriter();
            charWriter.write(buffer, defaultCharacter.data.data, writerResolver);
        } else {
            booleanWriter.write(buffer, false, writerResolver);
        }
    }
}

class TBinWriter {
    write(buffer, tBinData, writerResolver) {
        buffer.writeInt32LE(tBinData.data.length);
        buffer.concat(tBinData.data);
    }
}

class Vector3Writer {
    write(buffer, vector, writerResolver) {
        buffer.writeFloatLE(vector.x);
        buffer.writeFloatLE(vector.y);
        buffer.writeFloatLE(vector.z);
    }
}

class CharWriter {
    write(buffer, char, writerResolver) {
        assert.equal(char.length, 1);
        let charBuffer = new Buffer(4);
        let size = charBuffer.write(char);
        buffer.concat(charBuffer.slice(0, size));
    }
}

class StringWriter {
    write(buffer, text, writerResolver) {
        let stringBuffer = new Buffer(text.length * 2);
        let size = stringBuffer.write(text);
        buffer.write7BitEncodedNumber(size);
        buffer.concat(stringBuffer.slice(0, size));
    }
}

exports.StringWriter = StringWriter;

class RectangleWriter {
    write(buffer, rectangle, writerResolver) {
        buffer.writeInt32LE(rectangle.x);
        buffer.writeInt32LE(rectangle.y);
        buffer.writeInt32LE(rectangle.width);
        buffer.writeInt32LE(rectangle.height);
    }
}

class Int32Writer {
    write(buffer, number, writerResolver) {
        buffer.writeInt32LE(Number(number));
    }
}

class BooleanWriter {
    write(buffer, boolean, writerResolver) {
        buffer.writeByte(Boolean(boolean) ? 1 : 0);
    }
}

class WriterResolver {
    constructor(readers) {
        this.readerData = {};
        for(let i = 0; i < readers.length; i++) {
            let readerType = readers[i].type;

            let simpleType = util.simplifyType(readerType);
            this.readerData[simpleType] = {
                writer: getWriter(simpleType),
                valueType: reader.getReader(simpleType).isValueType(),
                index: i
            };
        }
    }

    write(buffer, value) {
        let readerData = this.readerData[value.type];
        if(!readerData.valueType) {
            buffer.write7BitEncodedNumber(readerData.index + 1);
        }
        readerData.writer.write(buffer, value.data, this);
    }
}

exports.WriterResolver = WriterResolver;

function getWriter(type) {
    let typeInfo = util.getTypeInfo(type);
    switch(typeInfo.type) {
        case 'Dictionary':
            return new DictionaryWriter(typeInfo.subtypes[0], typeInfo.subtypes[1]);

        case 'Array':
        case 'List':
            return new ArrayWriter(typeInfo.subtypes[0]);

        case 'Texture2D':
            return new Texture2DWriter();

        case 'Vector3':
            return new Vector3Writer();

        case 'String':
            return new StringWriter();

        case 'Int32':
            return new Int32Writer();

        case 'Char':
            return new CharWriter();

        case 'Boolean':
            return new BooleanWriter();

        case 'SpriteFont':
            return new SpriteFontWriter();

        case 'Rectangle':
            return new RectangleWriter();

        case 'TBin':
            return new TBinWriter();

        default:
            throw new util.ReadError('Non-implemented file writer for "' + type + '"');
    }
}
