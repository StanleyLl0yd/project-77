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

        [Test]
        public void NonInteractiveStates_CannotRestartOrModify()
        {
            Assert.That(
                PrototypeAttemptPolicy.CanRestartAttempt(PuzzleRunStatus.NotLoaded, continuationOffered: false),
                Is.False);
            Assert.That(
                PrototypeAttemptPolicy.CanModifyActiveAttempt(PuzzleRunStatus.NotLoaded, continuationOffered: false),
                Is.False);
            Assert.That(
                PrototypeAttemptPolicy.CanRestartAttempt(PuzzleRunStatus.Ready, continuationOffered: false),
                Is.False);
            Assert.That(
                PrototypeAttemptPolicy.CanModifyActiveAttempt(PuzzleRunStatus.Ready, continuationOffered: false),
                Is.False);
        }
    }
}
