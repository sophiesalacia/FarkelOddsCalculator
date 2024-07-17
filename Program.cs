// See https://aka.ms/new-console-template for more information

using System.Diagnostics;

Console.WriteLine("\nGenerating all possible hands now.");

List<List<Hand>> handSizeLists = new();


Stopwatch timer = Stopwatch.StartNew();
handSizeLists.Add(HandGenerator.GenerateAllCombinations(6));
handSizeLists.Add(HandGenerator.GenerateAllCombinations(5));
handSizeLists.Add(HandGenerator.GenerateAllCombinations(4));
handSizeLists.Add(HandGenerator.GenerateAllCombinations(3));
handSizeLists.Add(HandGenerator.GenerateAllCombinations(2));
handSizeLists.Add(HandGenerator.GenerateAllCombinations(1));
timer.Stop();

int totalHands = handSizeLists.Sum(hand => hand.Count);
Console.WriteLine($"Generated {totalHands} hands in {timer.ElapsedMilliseconds / 1000f} seconds.");

foreach (List<Hand> handList in handSizeLists)
{
	Console.WriteLine();
	int scoring = handList.Count(hand => hand.IsScoring);
	Console.WriteLine($"Odds of rolling a scoring hand with {6 - handSizeLists.IndexOf(handList)} dice: {scoring} / {handList.Count} ({(float)scoring / handList.Count * 100:F3}%)");
	Console.WriteLine("Per hand type:\n");

	for (int i = 0; i < 15; i++)
	{
		Hand.ScoringRule rule = (Hand.ScoringRule)i;

		//int handsWithRule = handList.Count(hand => hand.ScoreTypes.Contains(rule));
		// test this
		int handsWithRule = handList.Count(hand => hand.ScoreTypes[0] == rule);

		if (handsWithRule <= 0)
			continue;

		Console.WriteLine($"{rule}: {handsWithRule} / {handList.Count} ({(float)handsWithRule / handList.Count * 100:F3}%)");
	}
}

// Expected value
Console.WriteLine();

for (int i = 0; i < handSizeLists.Count; i++)
{
	float probability = 1 / (float)handSizeLists[i].Count;

	float expectedValue = handSizeLists[i].Sum(hand => hand.ScoreValue * probability);

	Console.WriteLine($"Expected value of a hand with {6 - i} dice: {expectedValue:f1}");
}

//Console.WriteLine("\n\nTest runs:");
//Random random = new();
//
//for (int i = 1; i < 11; i++)
//{
//	Console.WriteLine($"\n### RUN {i} ###");
//
//	Hand randomHand = handsOf6[random.Next(handsOf6.Count)];
//	Console.WriteLine();
//	randomHand.OutputHandInfo();
//
//	while (randomHand.IsScoring && randomHand.DiceLeftOver > 0)
//	{
//		randomHand = randomHand.DiceLeftOver switch
//		{
//			6 => handsOf6[random.Next(handsOf6.Count)],
//			5 => handsOf5[random.Next(handsOf5.Count)],
//			4 => handsOf4[random.Next(handsOf4.Count)],
//			3 => handsOf3[random.Next(handsOf3.Count)],
//			2 => handsOf2[random.Next(handsOf2.Count)],
//			1 => handsOf1[random.Next(handsOf1.Count)],
//			_ => randomHand
//		};
//
//		Console.WriteLine();
//		randomHand.OutputHandInfo();
//	}
//}




internal class HandGenerator
{
	private static int _counter = 0;

	internal static List<Hand> GenerateAllCombinations(int handSize)
	{
		List<Hand> hands = new();
		List<int> initialHand = new();

		GenerateCombinations(hands, initialHand, handSize);

		return hands;
	}

	internal static void GenerateCombinations(List<Hand> hands, List<int> hand, int handSize)
	{
		if (hand.Count == handSize)
		{
			
			Hand toAdd = new(hand);
			hands.Add(toAdd);

			//_counter++;
			//if (_counter > 823)
			//{
			//	Hand diagnostic = new(hand);
			//
			//	_counter = 0;
			//	OutputHandInfo(toAdd);
			//}

		}
		else
		{
			for (int i = 1; i < 7; i++)
			{
				List<int> newHand = new(hand) {i};
				GenerateCombinations(hands, newHand, handSize);
			}
		}
	}

}

internal class Hand
{
	private readonly List<int> _dice;
	internal int DiceCount => _dice.Count;

	internal enum ScoringRule
	{
		None,			// hand does not score
		Base,			// 1s or 5s counted individually
		ThreeOnes,		// 300 by base rules, 1000 by house rules
		ThreeTwos,
		ThreeThrees,
		ThreeFours,
		ThreeFives,
		ThreeSixes,
		FourOfAKind,
		FiveOfAKind,
		SixOfAKind,
		LargeStraight,
		SmallStraight,	// not in base rules, 750 by house rules
		ThreePairs,		// four of a kind + a pair = three pairs
		TwoTriplets
	}

	internal bool IsScoring;
	internal int ScoreValue; 
	internal List<ScoringRule> ScoreTypes = new();

	internal int DiceLeftOver;

	internal Hand(List<int> dice)
	{
		_dice = dice;
		DetermineScore();
	}

	internal void DetermineScore()
	{
		Dictionary<int, int> faceFrequency = GetFaces();
		
		ScoreTypes.Clear();
		DiceLeftOver = DiceCount;

		// 6 of a kind
		if (faceFrequency.ContainsValue(6))
		{
			IsScoring = true;
			ScoreValue = 3000;
			ScoreTypes.Add(ScoringRule.SixOfAKind);
			DiceLeftOver = 0;
			return;
		}

		// 5 of a kind
		if (faceFrequency.ContainsValue(5))
		{
			int face = faceFrequency.Single(pair => pair.Value == 5).Key;
			IsScoring = true;
			ScoreValue = 2000;
			ScoreTypes.Add(ScoringRule.FiveOfAKind);
			DiceLeftOver = DiceCount - 5;
			faceFrequency[face] = 0;

			if (DiceLeftOver > 0 && (faceFrequency[1] > 0 || faceFrequency[5] > 0))
			{
				IsScoring = true;
				ScoreValue += faceFrequency[1] * 100 + faceFrequency[5] * 50;
				ScoreTypes.Add(ScoringRule.Base);
				DiceLeftOver -= faceFrequency[1] + faceFrequency[5];
			}

			return;
		}

		// 4 of a kind or 3 pairs
		if (faceFrequency.ContainsValue(4))
		{
			// 4 of a kind + a pair = 3 pairs
			if (faceFrequency.ContainsValue(2))
			{
				IsScoring = true;
				ScoreValue = 1500;
				ScoreTypes.Add(ScoringRule.ThreePairs);
				DiceLeftOver = 0;
			}
			else
			{
				int face = faceFrequency.Single(pair => pair.Value == 4).Key;
				IsScoring = true;
				ScoreValue = 1000;
				ScoreTypes.Add(ScoringRule.FourOfAKind);
				DiceLeftOver = DiceCount - 4;
				faceFrequency[face] = 0;

				if (DiceLeftOver > 0 && (faceFrequency[1] > 0 || faceFrequency[5] > 0))
				{
					IsScoring = true;
					ScoreValue += faceFrequency[1] * 100 + faceFrequency[5] * 50;
					ScoreTypes.Add(ScoringRule.Base);
					DiceLeftOver -= faceFrequency[1] + faceFrequency[5];
				}
			}

			return;
		}

		// 3 of a kind or two triplets
		if (faceFrequency.ContainsValue(3))
		{
			if (faceFrequency.Count(pair => pair.Value == 3) > 1)
			{
				IsScoring = true;
				ScoreValue = 2500;
				ScoreTypes.Add(ScoringRule.TwoTriplets);
				DiceLeftOver = 0;

				return;
			}

			int face = faceFrequency.Single(pair => pair.Value == 3).Key;

			switch (face)
			{
				case 1:
					ScoreValue = HouseRules.BuffedThreeOnes ? 1000 : 300;
					ScoreTypes.Add(ScoringRule.ThreeOnes);
					break;

				case 2:
					ScoreValue = 200;
					ScoreTypes.Add(ScoringRule.ThreeTwos);
					break;

				case 3:
					ScoreValue = 300;
					ScoreTypes.Add(ScoringRule.ThreeThrees);
					break;

				case 4:
					ScoreValue = 400;
					ScoreTypes.Add(ScoringRule.ThreeFours);
					break;

				case 5:
					ScoreValue = 500;
					ScoreTypes.Add(ScoringRule.ThreeFives);
					break;

				case 6:
					ScoreValue = 600;
					ScoreTypes.Add(ScoringRule.ThreeSixes);
					break;

				default: throw new Exception("Unknown face value.");
			}
			
			IsScoring = true;
			faceFrequency[face] = 0;
			DiceLeftOver -= 3;

			// check for leftover 1s and 5s after 3 of a kind
			if (faceFrequency[1] > 0 || faceFrequency[5] > 0)
			{
				IsScoring = true;
				ScoreValue += faceFrequency[1] * 100 + faceFrequency[5] * 50;
				ScoreTypes.Add(ScoringRule.Base);
				DiceLeftOver -= faceFrequency[1] + faceFrequency[5];
			}

			return;
		}

		// 3 pairs
		if (faceFrequency.Count(pair => pair.Value == 2) == 3)
		{
			IsScoring = true;
			ScoreValue = 2500;
			ScoreTypes.Add(ScoringRule.ThreePairs);
			DiceLeftOver = 0;

			return;
		}

		// large straight
		if (faceFrequency.Count(pair => pair.Value > 0) == 6)
		{
			IsScoring = true;
			ScoreValue = 1500;
			ScoreTypes.Add(ScoringRule.LargeStraight);
			DiceLeftOver = 0;

			return;
		}

		// small straight
		if (HouseRules.SmallStraight && faceFrequency.Count(pair => pair.Value > 0) == 5)
		{
			if (faceFrequency[1] == 0)
			{
				// 2-6 small straight - check for additional 5
				IsScoring = true;
				ScoreValue = 750;
				ScoreTypes.Add(ScoringRule.SmallStraight);
				DiceLeftOver -= 5;
				if (DiceLeftOver > 0 && faceFrequency[5] > 1)
				{
					ScoreValue += 50;
					ScoreTypes.Add(ScoringRule.Base);
					DiceLeftOver = 0;
				}

				return;
			}
			
			if (faceFrequency[6] == 0)
			{
				// 1-5 small straight - check for additional 1 or 5
				IsScoring = true;
				ScoreValue = 750;
				ScoreTypes.Add(ScoringRule.SmallStraight);
				DiceLeftOver -= 5;
				
				if (DiceLeftOver > 0 && (faceFrequency[1] > 1 || faceFrequency[5] > 1))
				{
					IsScoring = true;
					ScoreValue += (faceFrequency[1] - 1) * 100 + (faceFrequency[5] - 1) * 50;
					ScoreTypes.Add(ScoringRule.Base);
					DiceLeftOver = 0;
				}

				return;
			}
		}

		// ones and fives
		if (faceFrequency[1] > 0 || faceFrequency[5] > 0)
		{
			IsScoring = true;
			ScoreValue = faceFrequency[1] * 100 + faceFrequency[5] * 50;
			ScoreTypes.Add(ScoringRule.Base);
			DiceLeftOver -= faceFrequency[1] + faceFrequency[5];

			return;
		}

		IsScoring = false;
		ScoreValue = -500;
		ScoreTypes.Add(ScoringRule.None);
	}

	private Dictionary<int, int> GetFaces()
	{
		Dictionary<int, int> faces = new()
		{
			[1] = 0,
			[2] = 0,
			[3] = 0,
			[4] = 0,
			[5] = 0,
			[6] = 0,
		};

		foreach (int face in _dice)
		{
			faces[face]++;
		}

		return faces;
	}

	internal void OutputHandInfo()
	{
		Console.WriteLine($"Hand: {this}");
		Console.WriteLine($"Is scoring: {IsScoring}");
		Console.WriteLine($"Score value: {ScoreValue}");
		Console.WriteLine($"Scoring type: {string.Join(", ", ScoreTypes)}");
		Console.WriteLine($"Dice remaining after scoring: {DiceLeftOver}");
	}

	public override string ToString()
	{
		_dice.Sort();
		return string.Join(" ", _dice);
	}
}

internal static class HouseRules
{
	internal static bool SmallStraight = true;
	internal static bool BuffedThreeOnes = true;
}