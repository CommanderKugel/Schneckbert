using System.Buffers.Binary;
using static Constants;
using static Utils;

public unsafe struct bullet
{

    public ulong occ;
    public fixed byte pieces[16];
    public short score;
    public byte result;
    public byte ksq;
    public byte opp_ksq;
    public fixed byte padding[3];


    public bullet(ref pos p, int score, Gameresult res)
    {
        occ     = p.get_blocker();
        result  = res switch { Gameresult.WinBlack => 0, Gameresult.Draw => 1, Gameresult.WinWhite => 2, _ => 1 };

        if (p.us == BLACK)
        {
            occ = BinaryPrimitives.ReverseEndianness(occ);
            result = (byte)(2 - result);
        }

        score   = (short)score;
        ksq     = (byte)p.get_ksq(p.us);
        opp_ksq = (byte)p.get_ksq(1-p.us);

        ulong temp = p.get_blocker();
        for (int idx=0; temp != 0; idx++)
        {
            int sq = popLsb(ref temp);
            if (p.us == BLACK) sq ^= 56;

            int pt = p.piece_on(sq);
            int color = p.color_on(sq);

            pieces[idx] = (byte)(color << 3 | pt);
        }
    }

    /// <summary>
    /// Well... writes the Bulletfomat-struct to a file.
    /// Writes to binary file and is not meant for human readability!
    /// File should be '.bullet' for clarity.
    /// </summary>
    public unsafe void write_to_file(BinaryWriter file)
    {
        file.Write(occ);
        for (int i=0; i<16; i++)
            file.Write(pieces[i]);
        file.Write(score);
        file.Write(result);
        file.Write(ksq);
        file.Write(opp_ksq);
        for (int i=0; i<3; i++)
            file.Write(padding[i]);
    }
}
