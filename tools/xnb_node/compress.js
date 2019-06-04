'use strict';
let ref = require('ref');
let ffi = require('ffi');
let Struct = require('ref-struct');
let path = require('path');

let intPtr = ref.refType('int');
let bytePtr = ref.refType('byte');

let XMEMCODEC_TYPE = {
    XMEMCODEC_DEFAULT: 0,
    XMEMCODEC_LZX: 1
};

let XMEMCODEC_PARAMETERS_LZX = Struct([
    ['int', 'Flags'],
    ['int', 'WindowSize'],
    ['int', 'CompressionPartitionSize']
]);

let XMEMCODEC_PARAMETERS_LZXPtr = ref.refType(XMEMCODEC_PARAMETERS_LZX);


let dllPath = path.join(__dirname, 'xcompress32.dll');
let xcompress = new ffi.Library(dllPath, {
    'XMemCompress': ['int', ['int', bytePtr, intPtr, bytePtr, 'int']],
    'XMemCreateCompressionContext': ['int', ['int', XMEMCODEC_PARAMETERS_LZXPtr, 'int', intPtr]],
    'XMemDestroyCompressionContext': ['void', ['int']],
    'XMemDecompress': ['int', ['int', bytePtr, intPtr, bytePtr, 'int']],
    'XMemCreateDecompressionContext': ['int', ['int', XMEMCODEC_PARAMETERS_LZXPtr, 'int', intPtr]],
    'XMemDestroyDecompressionContext': ['void', ['int']]
});

exports.decompress = function decompress(compressedBuffer, decompressedBuffer) {
    let codecParams = new XMEMCODEC_PARAMETERS_LZX({
        Flags: 0,
        WindowSize: 64 * 1024,
        CompressionPartitionSize: 256 * 1024
    });

    let decompressionContextRef = new Buffer(4);
    decompressionContextRef.type = ref.types.int;

    xcompress.XMemCreateDecompressionContext(XMEMCODEC_TYPE.XMEMCODEC_LZX, codecParams.ref(), 0, decompressionContextRef);

    let decompressedSizeRef = new Buffer(4);
    decompressedSizeRef.type = ref.types.int;
    decompressedSizeRef.writeInt32LE(decompressedBuffer.length, 0);

    xcompress.XMemDecompress(decompressionContextRef.readInt32LE(0), decompressedBuffer, decompressedSizeRef, compressedBuffer, compressedBuffer.length);
    xcompress.XMemDestroyDecompressionContext(decompressionContextRef.readInt32LE(0));
};

exports.compress = function compress(decompressedBuffer) {
    let codecParams = new XMEMCODEC_PARAMETERS_LZX({
        Flags: 0,
        WindowSize: 64 * 1024,
        CompressionPartitionSize: 256 * 1024
    });

    let compressionContextRef = new Buffer(4);
    compressionContextRef.type = ref.types.int;

    xcompress.XMemCreateCompressionContext(XMEMCODEC_TYPE.XMEMCODEC_LZX, codecParams.ref(), 0, compressionContextRef);

    let compressedSizeRef = new Buffer(4);
    compressedSizeRef.type = ref.types.int;
    compressedSizeRef.writeInt32LE(decompressedBuffer.length * 2, 0);

    let compressedBuffer = new Buffer(compressedSizeRef.readInt32LE(0));
    compressedBuffer.type = ref.types.byte;

    xcompress.XMemCompress(compressionContextRef.readInt32LE(0), compressedBuffer, compressedSizeRef, decompressedBuffer, decompressedBuffer.length);
    xcompress.XMemDestroyCompressionContext(compressionContextRef.readInt32LE(0));

    let resized = compressedBuffer.slice(0, compressedSizeRef.readInt32LE(0));
    return resized;
};
