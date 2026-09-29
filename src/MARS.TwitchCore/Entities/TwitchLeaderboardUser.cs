using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MARS.TwitchCore.Entities;

public class TwitchLeaderboardUser
{
    [Key]
    [Required]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required string TwitchId { get; set; }

    [ForeignKey(nameof(TwitchId))]
    public TwitchUser? TwitchUser { get; set; }

    [NotMapped]
    public int TotalWins => RussianRouletteWins + TriviaWins;
    public int RussianRouletteWins { get; set; }
    public int RussianRouletteWinsWithWaifu { get; set; }
    public int TriviaWins { get; set; }
    public int TriviaWinsWithWaifus { get; set; }
}
