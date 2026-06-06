using Xunit;

namespace OrderProcessor.UnitTests;

// The state machine is pure (no I/O), so we test it exhaustively and fast — the
// base of the test pyramid. These are [Theory] tests: one method, many cases.
public class OrderStateMachineTests
{
    [Theory]
    [InlineData("PLACED", "CONFIRM", "CONFIRMED")]
    [InlineData("CONFIRMED", "PREPARE", "PREPARING")]
    [InlineData("PREPARING", "DISPATCH", "DISPATCHED")]
    [InlineData("DISPATCHED", "DELIVER", "DELIVERED")]
    public void Legal_transitions_advance_state(string from, string command, string expected)
        => Assert.Equal(expected, OrderStateMachine.Next(from, command));

    [Theory]
    [InlineData("PLACED", "DELIVER")]     // can't deliver before dispatch
    [InlineData("DISPATCHED", "CANCEL")]  // can't cancel after dispatch
    [InlineData("DELIVERED", "CONFIRM")]  // terminal state
    [InlineData("PREPARING", "DELIVER")]  // skips a step
    public void Illegal_transitions_return_null(string from, string command)
        => Assert.Null(OrderStateMachine.Next(from, command));

    [Theory]
    [InlineData("PLACED")]
    [InlineData("CONFIRMED")]
    [InlineData("PREPARING")]
    public void Cancel_is_legal_before_dispatch(string from)
        => Assert.Equal("CANCELLED", OrderStateMachine.Next(from, "CANCEL"));
}
