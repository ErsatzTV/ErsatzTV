// the number of parts that each un-split file typically contains
// noinspection ES6ConvertVarToLetConst
var numParts = 3;

// return the part number for the given season number and episode number
function partNumberForEpisode(seasonNumber, episodeNumber) {
    const mod = episodeNumber % 3;
    return mod === 0 ? 3 : mod;
}
