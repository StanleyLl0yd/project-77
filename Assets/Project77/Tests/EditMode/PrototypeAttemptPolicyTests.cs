using NUnit.Framework;
using Project77.Puzzle;

namespace Project77.Tests
{
    public sealed class PrototypeAttemptPolicyTests
    {
        [Test]
        public void ActiveAttempt_CanRestartAndModifyBeforeContinuationOffer()
        {
            Assert.That(
                PrototypeAttemptPolicy.CanRestartAttempt(PuzzleRunStatus.Active, continuationOffered: false),
                Is.True);
            Assert.That(
                PrototypeAttemptPolicy.CanModifyActiveAttempt(PuzzleRunStatus.Active, continuationOffered: false),
                Is.True);
        }

        [Test]
        public void ContinuationOffer_LocksCompletedAttemptControls()
        {
            Assert.That(
                PrototypeAttemptPolicy.CanRestartAttempt(PuzzleRunStatus.Succeeded, continuationOffered: true),
                Is.False);
            Assert.That(
                PrototypeAttemptPolicy.CanModifyActiveAttempt(PuzzleRunStatus.Succeeded, continuationOffered: true),
                Is.False);
        }

        [Test]
        public void FailedAttempt_CanRestartButCannotBeModifiedUntilRetryStarts()
        {
            Assert.That(
                PrototypeAttemptPolicy.CanRestartAttempt(PuzzleRunStatus.Failed, continuationOffered: false),
                Is.True);
            Assert.That(
                PrototypeAttemptPolicy.CanModifyActiveAttempt(PuzzleRunStatus.Failed, continuationOffered: false),
                Is.False);
        }

        [TestCase(PuzzleRunStatus.NotLoaded)]
        [TestCase(PuzzleRunStatus.Ready)]
        public void NonInteractiveState_CannotRestartOrModify(PuzzleRunStatus status)
        {
            Assert.That(PrototypeAttemptPolicy.CanRestartAttempt(status, continuationOffered: false), Is.False);
            Assert.That(PrototypeAttemptPolicy.CanModifyActiveAttempt(status, continuationOffered: false), Is.False);
        }
    }
}
