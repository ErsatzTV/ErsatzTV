-- the number of parts that each un-split file typically contains
numParts = 3

-- return the part number for the given season number and episode number
function partNumberForEpisode(seasonNumber, episodeNumber)
	local mod = episodeNumber % 3
	if mod == 0
	then
		return 3
	else
		return mod
	end
end
