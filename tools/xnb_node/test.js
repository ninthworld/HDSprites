'use strict';

let fs = require('fs');
let mkdirp = require('mkdirp');
let walk = require('walk');
let path = require('path');
let assert = require('assert');
let converter = require('./converter');
let util = require('./util');
let typeyaml = require('./typeyaml');

let inputDir = '.\\content';
// let outputDir = '.\\out';

let walker = walk.walk(inputDir);
walker.on('file', function(root, fileStats, next) {
    let ext = path.extname(fileStats.name);
    if(ext.toLowerCase() == '.xnb') {
        let sourceFile = path.join(root, fileStats.name);
        let original = fs.readFileSync(sourceFile);
        let extracted;

        console.log(sourceFile);
        try {
            extracted = converter.XnbToObject(original);
        } catch(e) {
            if(e instanceof util.ReadError) {
                console.log(e.message);
                return next();
            } else {
                throw e;
            }
        }

        let yaml = typeyaml.stringify(extracted, 4);
        let parsed = typeyaml.parse(yaml);

        let repacked = converter.ObjectToXnb(parsed);
        if(!original.equals(repacked)) {
            console.log('First Pass Fail');
            let reExtracted = converter.XnbToObject(repacked);

            // if(extracted.content.data.data) {
            //     fs.writeFileSync('test.bin', extracted.content.data.data);
            //     fs.writeFileSync('test2.bin', reExtracted.content.data.data);
            //     delete extracted.content.data.data;
            //     delete reExtracted.content.data.data;
            // }
            // fs.writeFileSync('extracted.json', JSON.stringify(extracted));
            // fs.writeFileSync('extracted2.json', JSON.stringify(reExtracted));

            if(extracted.content.type == 'Texture2D') {
                assert.deepEqual(extracted.xnbData, reExtracted.xnbData);

                let left = extracted.content.data.data;
                let right = reExtracted.content.data.data;

                for(let i = 0; i < left.length; i++) {
                    let l = left[i];
                    let r = right[i];
                    assert.equal(Math.abs(l - r) <= 1, true);
                }
            } else {
                assert.deepEqual(extracted, reExtracted);
            }
        }

    } else {
        console.log('~' + path.join(root, fileStats.name));
    }
    next();
});
