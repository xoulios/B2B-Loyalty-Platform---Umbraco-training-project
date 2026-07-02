using KioskRewards.Domain.Entities;
using KioskRewards.Domain.Enums;
using KioskRewards.Domain.Exceptions;

namespace KioskRewards.Tests.Domain;

/// <summary>
/// Pure domain tests, no db, no Umbraco - just checking the aggregate enforces its own rules.
/// </summary>
public class PointsAccountTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Member = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static PointsAccount NewAccount() => PointsAccount.Create(Member);

    [Fact]
    public void Create_starts_with_zero_balance_and_empty_ledger()
    {
        var account = NewAccount();

        Assert.Equal(Member, account.MemberKey);
        Assert.Equal(0, account.Balance);
        Assert.Empty(account.Transactions);
    }

    [Fact]
    public void Create_with_empty_member_key_throws()
    {
        Assert.Throws<ArgumentException>(() => PointsAccount.Create(Guid.Empty));
    }

    [Fact]
    public void Earn_increases_balance_and_records_an_earn_entry()
    {
        var account = NewAccount();

        var tx = account.Earn(100, "First sale", Now);

        Assert.Equal(100, account.Balance);
        Assert.Single(account.Transactions);
        Assert.Equal(TransactionType.Earn, tx.Type);
        Assert.Equal(100, tx.Amount);
        Assert.Equal(Member, tx.MemberKey);
        Assert.Equal(Now, tx.CreatedUtc);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void Earn_with_non_positive_amount_throws(int amount)
    {
        var account = NewAccount();

        Assert.Throws<ArgumentOutOfRangeException>(() => account.Earn(amount, "bad", Now));
        Assert.Equal(0, account.Balance);
    }

    [Fact]
    public void Redeem_with_sufficient_balance_decreases_balance_and_records_a_redeem_entry()
    {
        var account = NewAccount();
        account.Earn(100, "earn", Now);

        var tx = account.Redeem(30, "Coffee mug", Now);

        Assert.Equal(70, account.Balance);
        Assert.Equal(2, account.Transactions.Count);
        Assert.Equal(TransactionType.Redeem, tx.Type);
        Assert.Equal(30, tx.Amount);
    }

    [Fact]
    public void Redeem_more_than_balance_throws_and_leaves_state_unchanged()
    {
        var account = NewAccount();
        account.Earn(20, "earn", Now);

        var ex = Assert.Throws<InsufficientPointsException>(() => account.Redeem(50, "too expensive", Now));

        Assert.Equal(20, ex.Balance);
        Assert.Equal(50, ex.Requested);
        Assert.Equal(20, account.Balance);          // unchanged
        Assert.Single(account.Transactions);        // no redeem entry was added
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Redeem_with_non_positive_cost_throws(int cost)
    {
        var account = NewAccount();
        account.Earn(100, "earn", Now);

        Assert.Throws<ArgumentOutOfRangeException>(() => account.Redeem(cost, "bad", Now));
        Assert.Equal(100, account.Balance);
    }

    [Theory]
    [InlineData(50, 50, true)]   // exactly the balance is allowed
    [InlineData(50, 49, true)]
    [InlineData(50, 51, false)]  // one over is not
    [InlineData(50, 0, false)]   // zero is not a valid redemption
    public void CanRedeem_reflects_affordability(int balance, int cost, bool expected)
    {
        var account = NewAccount();
        account.Earn(balance, "earn", Now);

        Assert.Equal(expected, account.CanRedeem(cost));
    }

    [Fact]
    public void Balance_tracks_a_sequence_of_earns_and_redeems()
    {
        var account = NewAccount();

        account.Earn(100, "sale 1", Now);
        account.Earn(50, "sale 2", Now);
        account.Redeem(40, "reward A", Now);
        account.Redeem(60, "reward B", Now);

        Assert.Equal(50, account.Balance);
        Assert.Equal(4, account.Transactions.Count);
    }
}
