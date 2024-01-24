// adapted from https://github.com/RondeSC/Super_Shuffle

namespace ErsatzTV.Core.Scheduling;

public class SuperShuffle
{
    private int userSID = 1;
    private int si, r1, r2, r3, r4;
    private int randR, halfN, rx, rkey;
    
    public int Shuffle(int inx, int shuffleId, int listSize) {
        int si, hi, offset;
        int halfSize = listSize / 2;  // for 1/2 the processing range

        shuffleId += 131 * (inx / listSize);  // have inx overflows supported
        si = (inx % listSize);
        if (si < halfSize) {
            hi = si;
            offset = 0;
        } else {
            hi = listSize - 1 - si;
            offset = halfSize;
            halfSize = listSize - halfSize;
            shuffleId++;
        }

        hi = MillerShuffleE(hi, shuffleId, halfSize);     // use any STD MSA() shuffle (aka: a PRIG function)
        si = MillerShuffleE(hi + offset, userSID, listSize);  // indexing into the baseline shuffle
        return(si);
    }

    private int MillerShuffleE(int inx, int shuffleId, int listSize)
    {
        const int p1 = 24317, p2 = 32141, p3 = 63629; // good for shuffling >60,000 indexes

        shuffleId += 131 * (inx / listSize); // have inx overflow effect the mix
        si = (inx + shuffleId) % listSize; // cut the deck

        randR = shuffleId; //local randomizer
        r1 = randR % p3;
        r2 = randR % p1; // Now, per Chinese remainder theorem, (r1,r2,r3) will be a unique set
        r3 = randR % p2;
        r4 = randR % 2749;
        halfN = listSize / 2 + 1;
        rx = (randR / listSize) % listSize + 1;
        rkey = (randR / listSize / listSize) % listSize + 1;

        // perform the conditional multi-faceted mathematical mixing (on avg 2 5/6 shuffle ops done + 2 simple Xors)
        if (si % 3 == 0) si = ((si / 3 * p1 + r1) % ((listSize + 2) / 3)) * 3; // spin multiples of 3 
        if (si <= halfN)
        {
            si = (si + r3) % (halfN + 1);
            si = halfN - si;
        } // improves large permu distro

        if (si % 2 == 0) si = ((si / 2 * p2 + r2) % ((listSize + 1) / 2)) * 2; // spin multiples of 2 
        if (si < halfN) si = (si * p3 + r3) % halfN;

        if ((si ^ rx) < listSize) si ^= rx; // flip some bits with Xor
        si = (si * p3 + r4) % listSize; // a relatively prime gears churning operation
        if ((si ^ rkey) < listSize) si ^= rkey;

        return si;
    }
}
