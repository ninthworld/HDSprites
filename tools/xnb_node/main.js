'use strict';

var program = require('commander');
let mkdirp = require('mkdirp');
let walk = require('walk');
let path = require('path');
let fs = require('fs');
let converter = require('./converter');
let util = require('./util');
let typeyaml = require('./typeyaml');
let contenteditor = require('./contenteditor');

program
    .option('-q, --quiet', 'don\'t output files being processed')
    .version('0.2.1');

program
    .command('extract <input> <output>')
    .description('extract XNB file or all files in directory')
    .action(function(input, output) {
        applyOrRecurse(extractXnb, input, output);
    });

program
    .command('pack <input> <output>')
    .description('pack XNB file or all files in directory')
    .action((input, output) => {
        applyOrRecurse(packXnb, input, output);
    });

program
    .action(() => program.help());

program.parse(process.argv);

if(!process.argv.slice(2).length) {
    program.help();
}

function extractXnb(inputFile, outputFile) {
    let inputBuffer = fs.readFileSync(inputFile);
    let data = converter.XnbToObject(inputBuffer);
    mkdirp.sync(path.dirname(outputFile));
    data = contenteditor.onPresave(outputFile, data);
    fs.writeFileSync(outputFile, typeyaml.stringify(data, 4), 'utf8');
}

function packXnb(inputFile, outputFile) {
    let inputYaml = fs.readFileSync(inputFile, 'utf8');
    let data = typeyaml.parse(inputYaml);
    data = contenteditor.onPostload(inputFile, data);
    let xnb = converter.ObjectToXnb(data);
    mkdirp.sync(path.dirname(outputFile));
    fs.writeFileSync(outputFile, xnb);
}

function readErrorWrapper(fn) {
    return function() {
        try {
            fn.apply(this, arguments);
        } catch(e) {
            if(e instanceof util.ReadError) {
                return;
            } else {
                throw e;
            }
        }
    }
}

function applyOrRecurse(fn, input, output) {
    fn = readErrorWrapper(fn);

    let stats;
    try {
        stats = fs.statSync(input);
    } catch(e) {
        if(e.code === 'ENOENT') {
            return console.log(`The file or directory "${input}" was not found.`);
        } else {
            throw e;
        }
    }

    if(stats.isFile()) {
        fn(input, output);
    } else if(stats.isDirectory()) {
        let walker = walk.walk(input);
        walker.on('file', (root, fileStats, next) => {
            let ext = path.extname(fileStats.name).toLowerCase();
            if(ext != '.xnb' && ext != '.yaml') return next();

            let targetDir = root.replace(input, output);
            let sourceFile = path.join(root, fileStats.name);
            if(!program.quiet) console.log(sourceFile);

            let targetExt = ext == '.xnb' ? '.yaml' : '.xnb';
            let targetFile = path.join(targetDir, path.basename(fileStats.name, ext) + targetExt);

            fn(sourceFile, targetFile);
            next();
        });
    }
}
