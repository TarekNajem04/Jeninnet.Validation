//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation/
// Licensed under the MIT License.
//
import { readFileSync, writeFileSync } from "node:fs";
import { resolve } from "node:path";

const [inputPath, outputPath] = process.argv.slice(2);

if(!inputPath || !outputPath) {
    console.error("Usage: node normalize-lcov-paths.mjs <input-lcov> <output-lcov>");
    process.exit(2);
}

const text = readFileSync(resolve(inputPath), "utf8");
const normalized = text.replace(/^SF:(.+)$/gm, (_, filePath) => {
    const repositoryPath = filePath.replaceAll("\\", "/").replace(/^client\//, "");
    return `SF:client/${repositoryPath}`;
});

writeFileSync(resolve(outputPath), normalized, "utf8");
