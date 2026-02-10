using System;
using System.Collections.Generic;

public enum BrainrotQuestionType
{
    CharacterName,
    LoreWikipedia,
    VisualIdentification
}

public sealed class BrainrotTriviaQuestion
{
    public BrainrotTriviaQuestion(string prompt, IReadOnlyList<string> options, int correctIndex, BrainrotQuestionType type)
    {
        Prompt = prompt;
        Options = options;
        CorrectIndex = correctIndex;
        Type = type;
    }

    public string Prompt { get; }
    public IReadOnlyList<string> Options { get; }
    public int CorrectIndex { get; }
    public BrainrotQuestionType Type { get; }
}

public sealed class BrainrotTriviaBonusGame
{
    public const int QuestionsPerRound = 6;
    public const int OptionsPerQuestion = 4;
    public const float QuestionTimerSeconds = 7f;
    public const int CoinsPerCorrect = 1;
    public const int PerfectRoundBonusCoins = 1;
    public const int GrandQuestionCoins = 10;

    private readonly Queue<BrainrotTriviaQuestion> questions = new();
    private readonly BrainrotStarTracker starTracker;

    private BrainrotTriviaQuestion currentQuestion;
    private float questionTimeRemaining;
    private bool questionActive;
    private int correctAnswers;
    private int hiddenCoins;
    private bool grandQuestionUnlocked;

    public BrainrotTriviaBonusGame(BrainrotStarTracker tracker)
    {
        starTracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
    }

    public event Action<BrainrotTriviaQuestion, float> QuestionStarted;
    public event Action<int> CoinsRevealed;
    public event Action<int> BonusRoundEnded;
    public event Action<int, int> GrandQuestionResolved;

    public void BeginRound(IEnumerable<BrainrotTriviaQuestion> roundQuestions)
    {
        questions.Clear();
        foreach (var question in roundQuestions)
        {
            questions.Enqueue(question);
        }

        if (questions.Count != QuestionsPerRound)
            throw new InvalidOperationException($"Expected {QuestionsPerRound} questions for bonus round.");

        correctAnswers = 0;
        hiddenCoins = 0;
        grandQuestionUnlocked = false;
        AdvanceToNextQuestion();
    }

    public void Tick(float deltaTime)
    {
        if (!questionActive)
            return;

        questionTimeRemaining -= deltaTime;
        if (questionTimeRemaining <= 0f)
        {
            questionTimeRemaining = 0f;
            questionActive = false;
            ResolveAnswer(-1);
        }
    }

    public void SubmitAnswer(int selectedIndex)
    {
        if (!questionActive)
            return;

        questionActive = false;
        ResolveAnswer(selectedIndex);
    }

    public void SubmitGrandQuestionAnswer(int selectedIndex, BrainrotTriviaQuestion grandQuestion)
    {
        var isCorrect = selectedIndex == grandQuestion.CorrectIndex;
        var awardedCoins = isCorrect ? GrandQuestionCoins : 0;
        var awardedLives = isCorrect ? 1 : 0;
        if (isCorrect)
        {
            starTracker.AddCoins(GrandQuestionCoins);
            starTracker.AddExtraLife(1);
        }

        GrandQuestionResolved?.Invoke(awardedCoins, awardedLives);
    }

    public bool ShouldTriggerGrandQuestion() => grandQuestionUnlocked;

    public int RevealCoins()
    {
        CoinsRevealed?.Invoke(hiddenCoins);
        return hiddenCoins;
    }

    private void AdvanceToNextQuestion()
    {
        if (questions.Count == 0)
        {
            EndRound();
            return;
        }

        currentQuestion = questions.Dequeue();
        questionTimeRemaining = QuestionTimerSeconds;
        questionActive = true;
        QuestionStarted?.Invoke(currentQuestion, questionTimeRemaining);
    }

    private void ResolveAnswer(int selectedIndex)
    {
        if (selectedIndex == currentQuestion.CorrectIndex)
        {
            correctAnswers += 1;
            hiddenCoins += CoinsPerCorrect;
        }

        AdvanceToNextQuestion();
    }

    private void EndRound()
    {
        if (correctAnswers == QuestionsPerRound)
        {
            hiddenCoins += PerfectRoundBonusCoins;
            grandQuestionUnlocked = true;
        }

        starTracker.AwardBonusRoundStar();
        BonusRoundEnded?.Invoke(hiddenCoins);
    }
}

public sealed class BrainrotStarTracker
{
    public const int StarsPerChest = 3;
    public const int ChestConsumeCoinReward = 10;
    public const int ChestConsumeExtraLifeReward = 1;

    public readonly struct ChestConsumeResult
    {
        public ChestConsumeResult(bool consumed, int awardedCoins, int awardedLives, int starsBeforeConsume)
        {
            Consumed = consumed;
            AwardedCoins = awardedCoins;
            AwardedLives = awardedLives;
            StarsBeforeConsume = starsBeforeConsume;
        }

        public bool Consumed { get; }
        public int AwardedCoins { get; }
        public int AwardedLives { get; }
        public int StarsBeforeConsume { get; }
    }

    public int CurrentStars { get; private set; }
    public int StoredCoins { get; private set; }
    public int ExtraLives { get; private set; }
    public bool ChestReady { get; private set; }

    public event Action<int> StarsChanged;
    public event Action ChestReadyChanged;
    public event Action<int> ExtraLivesChanged;
    public event Action<int> CoinsChanged;
    public event Action<ChestConsumeResult> ChestConsumed;

    public void AwardBonusRoundStar()
    {
        if (ChestReady)
            return;

        CurrentStars += 1;
        StarsChanged?.Invoke(CurrentStars);

        if (CurrentStars >= StarsPerChest)
        {
            ChestReady = true;
            ExtraLives += 1;
            ExtraLivesChanged?.Invoke(ExtraLives);
            ChestReadyChanged?.Invoke();
        }
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0)
            return;

        StoredCoins += amount;
        CoinsChanged?.Invoke(StoredCoins);
    }

    public void AddExtraLife(int amount)
    {
        ExtraLives += amount;
        ExtraLivesChanged?.Invoke(ExtraLives);
    }

    public bool TryConsumeChestAtLevelEnd()
    {
        var consumed = ConsumeChest();
        if (!consumed)
            return false;

        AddCoins(ChestConsumeCoinReward);
        AddExtraLife(ChestConsumeExtraLifeReward);
        ChestConsumed?.Invoke(new ChestConsumeResult(true, ChestConsumeCoinReward, ChestConsumeExtraLifeReward, StarsPerChest));
        return true;
    }

    public bool TryAutoRetryOnDeath()
    {
        var starsBeforeConsume = CurrentStars;
        if (!ConsumeChest())
            return false;

        ChestConsumed?.Invoke(new ChestConsumeResult(true, 0, 0, starsBeforeConsume));
        return true;
    }

    public int getPlayerCoins() => StoredCoins;

    public void addCoins(int amount) => AddCoins(amount);

    public bool spendCoins(int itemCost)
    {
        if (itemCost <= 0 || StoredCoins < itemCost)
            return false;

        StoredCoins -= itemCost;
        CoinsChanged?.Invoke(StoredCoins);
        return true;
    }

    public bool triggerChestConsume() => TryConsumeChestAtLevelEnd();

    public int updateStarCount()
    {
        StarsChanged?.Invoke(CurrentStars);
        return CurrentStars;
    }

    private bool ConsumeChest()
    {
        if (!ChestReady)
            return false;

        ChestReady = false;
        CurrentStars = 0;
        StarsChanged?.Invoke(CurrentStars);
        ChestReadyChanged?.Invoke();
        return true;
    }
}
