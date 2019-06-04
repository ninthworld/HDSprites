'use strict';

let ref = require('ref');
let fs = require('fs');
let assert = require('assert');
let compress = require('./compress');
let reader = require('./reader');
let writer = require('./writer');
let util = require('./util');

function XnbToObject(inputBuffer) {
    let buffer = new reader.BufferConsumer(inputBuffer);

    //XNB File format
    assert.equal(buffer.consume(3).toString('ascii'), 'XNB');

    //XNB Target
    let target = buffer.consume(1).toString('ascii');

    //XNA Version 4
    assert.equal(buffer.consume(1).readInt8(0), 5);

    let flags = buffer.consume(1).readInt8(0);
    let isCompressed = flags & 0x80;
    let isHiDef = flags & 0x01;

    let compressedSize = buffer.consume(4).readUInt32LE(0);
    let decompressedSize = 0;

    if(isCompressed) {
        decompressedSize = buffer.consume(4).readUInt32LE(0);
    }

    let decompressedBuffer = new Buffer(decompressedSize);
    decompressedBuffer.type = ref.types.byte;

    if(isCompressed) {
        compress.decompress(buffer.buffer, decompressedBuffer);
    } else {
        decompressedBuffer = buffer.buffer;
    }

    // fs.writeFileSync('test.bin', decompressedBuffer);
    let content = new reader.BufferConsumer(decompressedBuffer);

    let stringReader = new reader.StringReader();

    let readers = [];
    let readerData = [];
    let numReaders = content.consume7BitEncodedNumber();
    for(let i = 0; i < numReaders; i++) {
        let readerType = stringReader.consume(content).data;
        let version = content.consume(4).readInt32LE();

        readerData.push({
            type: readerType,
            version: version
        });

        readers.push(reader.getReader(util.simplifyType(readerType)));
    }

    let numSharedResources = content.consume7BitEncodedNumber();
    assert.equal(numSharedResources, 0);

    //NOTE: It may happen that the main entry is a ValueType, in which case this would cause a mess.
    let readerResolver = new reader.ReaderResolver(readers);
    let result = readerResolver.consume(content);

    assert.equal(content.length, 0);

    let xnbData = {
        target: target,
        compressed: Boolean(isCompressed),
        hiDef: Boolean(isHiDef),
        readerData: readerData,
        numSharedResources: numSharedResources
    };

    return {
        xnbData: xnbData,
        content: result
    };
}

function ObjectToXnb(data) {
    let buffer = new writer.BufferWriter();

    buffer.writeAscii('XNB');
    buffer.writeAscii('w');
    buffer.writeByte(5);

    let flags = 0;
    if(data.xnbData.compressed) flags |= 0x80;
    if(data.xnbData.hiDef) flags |= 0x01;

    buffer.writeByte(flags);

    let decompressedBuffer = new writer.BufferWriter();

    let numReaders = data.xnbData.readerData.length;
    decompressedBuffer.write7BitEncodedNumber(numReaders);

    let stringWriter = new writer.StringWriter();

    for(let i = 0; i < numReaders; i++) {
        let readerData = data.xnbData.readerData[i];
        stringWriter.write(decompressedBuffer, readerData.type);
        decompressedBuffer.writeInt32LE(readerData.version);
    }

    decompressedBuffer.write7BitEncodedNumber(data.xnbData.numSharedResources);

    let writerResolver = new writer.WriterResolver(data.xnbData.readerData);
    writerResolver.write(decompressedBuffer, data.content);

    if(data.xnbData.compressed) {
        let compressedBuffer = new writer.BufferWriter(compress.compress(decompressedBuffer.buffer));
        buffer.writeInt32LE(buffer.length + compressedBuffer.length + 8);
        buffer.writeInt32LE(decompressedBuffer.length);
        buffer.concat(compressedBuffer.buffer);

    } else {
        buffer.writeInt32LE(buffer.length + decompressedBuffer.length + 4);
        buffer.concat(decompressedBuffer.buffer);
    }

    return buffer.buffer;
}

module.exports = {
    XnbToObject,
    ObjectToXnb
};

// let originalBuffer = fs.readFileSync('SmallFont.xnb');
// let result = XnbToJson(originalBuffer);
// fs.writeFileSync('result.json', result);
// let finalBuffer = JsonToXnb(result);
// fs.writeFileSync('result.xnb', finalBuffer);
// // // JsonToXnb('Clint2.json', 'Clint2.xnb');
// console.log(originalBuffer.equals(finalBuffer));
// //
