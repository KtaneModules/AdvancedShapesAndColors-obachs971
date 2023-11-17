using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PuzzleGenerator {
    private readonly int gridSize = 3;
    private readonly string letters = "RYB";
    private readonly string numbers = "CTD";
    private bool needUpdate = false;
    private string[][] sol;

    //private bool loop;
    // Returns an list of randomly generated clues
    public List<Clue> GeneratePuzzle(bool INHIBITOR_STATUS)
    {
        //looper:
        string[][] solution = getInitialSolution();
        sol = solution;
        tryagain:
        //loop = true;
        List<int> positions = getShuffledPositions();
        List<List<List<string>>> possible = getInitialPossible();
        List<string[][]> clues = new List<string[][]>();
        List<string[][]> negativeClues = new List<string[][]>();
        foreach (int position in positions)
        {
            int row = position / gridSize, col = position % gridSize;
            if(possible[row][col].Count > 1)
            {
               
                string[][] negClue = getNegativeClue(solution, possible, row, col);
                //negClue = null;
                if(negClue != null)
                {
                    negativeClues.Add(negClue);
                    //Debug.LogFormat("NEGATIVE");
                    //printClue(negClue);
                }
                else
                {
                    string[][] clue = getBlankClue();
                    string clueElement = getFirstClueElement(solution[row][col], possible[row][col]);
                    clue[row][col] = clueElement;
                    blackOutSpaces(possible, clue, row, col);   // Gen 3
                    clue = makeClueDistinct(possible, solution, clue);  // Gen 3.5
                    removePossibleCombinations(possible, clue); //Gen 2
                    removePossibleCombinations(possible);   // Gen 2
                    clues.Add(clue);
                    //Debug.LogFormat("POSITIVE");
                    //printClue(clue);
                }
                foreach (string[][] negativeClue in negativeClues)
                    removePossibleCombinationsNegative(possible, negativeClue); // Gen 2
                removePossibleCombinations(possible);   // Gen 2
            }
        }
        removeRedundantClues(clues, negativeClues);    // Gen 5
        removeRedundantClueElements(clues, negativeClues); // Gen 5
        removeEmptyClues(clues);    //Gen 8
        removeRedundantSpaces(clues, negativeClues);   // Gen 6
        bool convertedClue = negativesToPositives(clues, negativeClues, solution);
        while(convertedClue)
        {
            removeRedundantClues(clues, negativeClues);    // Gen 5
            removeRedundantClueElements(clues, negativeClues); // Gen 5
            removeEmptyClues(clues);    //Gen 8
            removeRedundantSpaces(clues, negativeClues);   // Gen 6
            convertedClue = negativesToPositives(clues, negativeClues, solution);
        }
        combineClues(clues, solution);  // Gen 4
        combineNegativeClues(negativeClues); // Gen 4
        while (needUpdate)
        {
            needUpdate = false;
            removeRedundantClues(clues, negativeClues);    // Gen 5
            removeRedundantClueElements(clues, negativeClues); // Gen 5
            removeEmptyClues(clues);    //Gen 8
            removeRedundantSpaces(clues, negativeClues);   // Gen 6
            convertedClue = negativesToPositives(clues, negativeClues, solution);
            while (convertedClue)
            {
                removeRedundantClues(clues, negativeClues);    // Gen 5
                removeRedundantClueElements(clues, negativeClues); // Gen 5
                removeEmptyClues(clues);    //Gen 8
                removeRedundantSpaces(clues, negativeClues);   // Gen 6
                convertedClue = negativesToPositives(clues, negativeClues, solution);
            }
            combineClues(clues, solution);  // Gen 4
            combineNegativeClues(negativeClues); // Gen 4
        }
        bool retry = true;
        foreach(string[][] clue in negativeClues)
        {
            if (clue.Length < 3 && clue[0].Length < 3)
            {
                List<int[]> elementPositions = getElementPositions(clue);
                if(elementPositions.Count > 1)
                {
                    retry = false;
                    break;
                }
            }
        }
        if (retry)
            goto tryagain;

        if (INHIBITOR_STATUS && !canSolve(clues, negativeClues))
            goto tryagain;
        

        for (int i = 0; i < clues.Count; i++)
            clues[i] = shrinkClue(clues[i]);
        turnSpacesBlack(clues);         // Gen 7
        turnSpacesBlack(negativeClues); // Gen 7
        List<Clue> allClues = new List<Clue>();
        foreach(string[][] clue in clues)
            allClues.Add(new Clue(clue, true));
        foreach(string[][] clue in negativeClues)
            allClues.Add(new Clue(clue, false));
        return allClues;
    }
    public string[][] getSolution()
    {
        return sol;
    }
    // Returns a randomly generated solution
    private string[][] getInitialSolution()
    {
        List<string> choices = new List<string>();
        foreach (char let in letters)
        {
            foreach (char num in numbers)
                choices.Add(let + "" + num);
        }
        choices.Shuffle();
        string[][] solution = new string[gridSize][];
        for (int i = 0; i < gridSize; i++)
            solution[i] = new string[gridSize];
        for (int i = 0; i < choices.Count; i++)
            solution[i / gridSize][i % gridSize] = choices[i];
        return solution;
    }
    // Returns a list of shuffled positions
    private List<int> getShuffledPositions()
    {
        List<int> positions = new List<int>();
        for (int i = 0; i < (gridSize * gridSize); i++)
            positions.Add(i);
        positions.Shuffle();
        return positions;
    }

    // Returns all possible combinations for each space in the grid
    private List<List<List<string>>> getInitialPossible()
    {
        List<List<List<string>>> possible = new List<List<List<string>>>();
        for(int i = 0; i < gridSize; i++)
        {
            possible.Add(new List<List<string>>());
            for(int j = 0; j < gridSize; j++)
            {
                possible[i].Add(new List<string>());
                foreach(char let in letters)
                {
                    foreach (char num in numbers)
                        possible[i][j].Add(let + "" + num);
                }
            }
        }
        return possible;
    }

    // Returns a blank clue grid
    private string[][] getBlankClue()
    {
        string[][] clue = new string[gridSize][];
        for(int i = 0; i < gridSize; i++)
        {
            clue[i] = new string[gridSize];
            for (int j = 0; j < gridSize; j++)
                clue[i][j] = "WW";
        }
        return clue;
    }
    // Returns a blank clue grid with specified sizes
    private string[][] getBlankClue(int row, int col)
    {
        string[][] clue = new string[row][];
        for (int i = 0; i < row; i++)
        {
            clue[i] = new string[col];
            for (int j = 0; j < col; j++)
                clue[i][j] = "WW";
        }
        return clue;
    }
    // Returns a clue element that reduces the number of possible combinations down to 1
    private string getFirstClueElement(string solution, List<string> possible)
    {
        List<string> choices = new List<string>();
        choices.Add(solution[0] + "");
        choices.Add(solution[1] + "");
        foreach (char c in letters.Replace(solution[0] + "", ""))
            choices.Add("-" + c);
        foreach (char c in numbers.Replace(solution[1] + "", ""))
            choices.Add("-" + c);
        choices.Shuffle();
        foreach(string choice in choices)
        {
            if (canUseClueElement(possible, choice))
                return choice;
        }
        return solution.ToUpperInvariant();
    }
    // Checks if the clue element reduces the number of possible combinations down to 1
    private bool canUseClueElement(List<string> possible, string element)
    {
        int sum = 0;
        if(element[0] == '-')
        {
            foreach(string str in possible)
            {
                if (!(str.Contains(element.Substring(1))))
                    sum++;
            }
        }
        else
        {
            foreach (string str in possible)
            {
                if (str.Contains(element))
                    sum++;
            }
        }
        return sum == 1;
    }
    // Removes possible combinations using the given clue
    private void removePossibleCombinations(List<List<List<string>>> possible, string[][] clue)
    {
        for(int i = 0; i < gridSize; i++)
        {
            for(int j = 0; j < gridSize; j++)
            {
                if(!(clue[i][j].Equals("WW") || clue[i][j].Equals("KK")))
                {
                    if(clue[i][j][0] == '-')
                    {
                        for(int k = 0; k < possible[i][j].Count; k++)
                        {
                            if (possible[i][j][k].Contains(clue[i][j].Substring(1)))
                                possible[i][j].RemoveAt(k--);
                        }
                    }
                    else
                    {
                        for (int k = 0; k < possible[i][j].Count; k++)
                        {
                            if (!(possible[i][j][k].Contains(clue[i][j])))
                                possible[i][j].RemoveAt(k--);
                        }
                    }
                }
            }
        }
    }
    // Removes possible combinations that have been reduced down to 1 spot on the grid
    private void removePossibleCombinations(List<List<List<string>>> possible)
    {
        bool flag = true;
        while (flag)
        {
            Dictionary<string, int> dict = new Dictionary<string, int>();
            for (int row = 0; row < possible.Count; row++)
            {
                for (int col = 0; col < possible[row].Count; col++)
                {
                    string temp = string.Join(" ", possible[row][col].ToArray());
                    if (dict.ContainsKey(temp))
                        dict[temp]++;
                    else
                        dict.Add(temp, 1);
                }
            }
            flag = false;
            foreach (string str in dict.Keys)
            {
                string[] list = str.Split(' ');
                if (list.Length == dict[str])
                {
                    for (int i = 0; i < gridSize; i++)
                    {
                        for (int j = 0; j < gridSize; j++)
                        {
                            if (!(list.SequenceEqual(possible[i][j].ToArray())))
                            {
                                for (int k = 0; k < list.Length; k++)
                                {
                                    if (possible[i][j].Remove(list[k]))
                                        flag = true;
                                }
                            }
                        }
                    }
                }
            }
            dict = getComboOcc(possible);
            int max = getMaxOcc(dict);
            for(int i = 1; i <= max; i++)
            {
                List<string> combos = new List<string>();
                foreach (string str in dict.Keys)
                {
                    if (dict[str] == i)
                        combos.Add(str);
                }
                for(int row = 0; row < gridSize; row++)
                {
                    for(int col = 0; col < gridSize; col++)
                    {
                        List<string> reduce = new List<string>();
                        foreach (string combo in combos)
                        {
                            if (possible[row][col].Contains(combo))
                                reduce.Add(combo);
                        }
                        if (reduce.Count == i && getComboCount(possible, reduce) == i)
                        {
                            for(int index = 0; index < possible[row][col].Count; index++)
                            {
                                if (!reduce.Contains(possible[row][col][index]))
                                {
                                    possible[row][col].RemoveAt(index);
                                    flag = true;
                                    index--;
                                }
                            }
                        }
                    }
                }
                dict = getComboOcc(possible);
                max = getMaxOcc(dict);
            }
        }
    }
    // Removes possible combinations that have been reduced down to 1 spot on the grid
    private void removePossibleCombinationsTest(List<List<List<string>>> possible)
    {
        bool flag = true;
        while (flag)
        {
            Dictionary<string, int> dict = new Dictionary<string, int>();
            for (int row = 0; row < possible.Count; row++)
            {
                for (int col = 0; col < possible[row].Count; col++)
                {
                    string temp = string.Join(" ", possible[row][col].ToArray());
                    if (dict.ContainsKey(temp))
                        dict[temp]++;
                    else
                        dict.Add(temp, 1);
                }
            }
            flag = false;
            foreach (string str in dict.Keys)
            {
                string[] list = str.Split(' ');
                if (list.Length == dict[str])
                {
                    for (int i = 0; i < gridSize; i++)
                    {
                        for (int j = 0; j < gridSize; j++)
                        {
                            if (!(list.SequenceEqual(possible[i][j].ToArray())))
                            {
                                for (int k = 0; k < list.Length; k++)
                                {
                                    if (possible[i][j].Remove(list[k]))
                                        flag = true;
                                }
                            }
                        }
                    }
                }
            }

            dict = getComboOcc(possible);
            int max = getMaxOcc(dict);
            for (int i = 1; i <= max; i++)
            {
                List<string> combos = new List<string>();
                foreach (string str in dict.Keys)
                {
                    if (dict[str] == i)
                        combos.Add(str);
                }
                // ARRRGH THIS IS SO COMPLICATED
                for (int row = 0; row < gridSize; row++)
                {
                    for (int col = 0; col < gridSize; col++)
                    {
                        List<string> reduce = new List<string>();
                        foreach (string combo in combos)
                        {
                            if (possible[row][col].Contains(combo))
                                reduce.Add(combo);
                        }
                        if (reduce.Count == i && getComboCount(possible, reduce) == i)
                        {
                            string str = "";
                            foreach (string combo in reduce)
                                str += combo + " ";
                            //Debug.LogFormat("Reduce Combos: {0}", str);
                            str = "";
                            foreach(string combo in possible[row][col])
                                str += combo + " ";
                            //Debug.LogFormat("Poss List: {0}", str);
                            for (int index = 0; index < possible[row][col].Count; index++)
                            {
                                if (!reduce.Contains(possible[row][col][index]))
                                {
                                    possible[row][col].RemoveAt(index);
                                    flag = true;
                                    index--;
                                }
                            }
                        }
                    }
                }
                dict = getComboOcc(possible);
                max = getMaxOcc(dict);
            }
        }
    }
    private int getComboCount(List<List<List<string>>> possible, List<string> combos)
    {
        int sum = 0;
        foreach(List<List<string>> row in possible)
        {
            foreach(List<string> col in row)
            {
                bool flag = true;
                foreach (string combo in combos)
                    flag = flag && col.Contains(combo);
                if (flag)
                    sum++;
            }
        }
        return sum;
    }
    private Dictionary<string, int> getComboOcc(List<List<List<string>>> possible)
    {
        Dictionary<string, int> dict = new Dictionary<string, int>();
        foreach (char l in letters)
        {
            foreach (char n in numbers)
                dict.Add(l + "" + n, 0);
        }
        foreach (List<List<string>> row in possible)
        {
            foreach (List<string> col in row)
            {
                foreach (string combo in col)
                    dict[combo]++;
            }
        }
        return dict;
    }
    private int getMaxOcc(Dictionary<string, int> dict)
    {
        int max = 0;
        foreach (string str in dict.Keys)
        {
            if (dict[str] > max)
                max = dict[str];
        }
        return max;
    }
    // Blacks out spaces on the clue based on what is filled around the clue
    private void blackOutSpaces(List<List<List<string>>> possible, string[][] clue, int row, int col)
    {
        int cur = gridSize - 1;
        for(int i = (row + 1); i < gridSize; i++)
        {
            if (possible[i][col].Count == 1)
            {
                for (int j = 0; j < gridSize; j++)
                    clue[cur][j] = "KK";
                cur--;
            }
            else
                break;
        }
        cur = gridSize - 1;
        for(int i = (col + 1); i < gridSize; i++)
        {
            if (possible[row][i].Count == 1)
            {
                for (int j = 0; j < gridSize; j++)
                    clue[j][cur] = "KK";
                cur--;
            }
            else
                break;
        }
        cur = 0;
        for (int i = (row - 1); i >= 0; i--)
        {
            if (possible[i][col].Count == 1)
            {
                for (int j = 0; j < gridSize; j++)
                    clue[cur][j] = "KK";
                cur++;
            }
            else
                break;
        }
        cur = 0;
        for (int i = (col - 1); i >= 0; i--)
        {
            if (possible[row][i].Count == 1)
            {
                for (int j = 0; j < gridSize; j++)
                    clue[j][cur] = "KK";
                cur++;
            }
            else
                break;
        }
    }

    // Makes the clue distinct, AKA ensures that the clue can only be placed in exactly 1 spot onto the grid
    private string[][] makeClueDistinct(List<List<List<string>>> possible, string[][] solution, string[][] clue)
    {
        bool flag = isDistinct(possible, clue);
        while(!(flag))
        {
            List<string[][]> choices = new List<string[][]>();
            for(int i = 0; i < gridSize; i++)
            {
                for(int j = 0; j < gridSize; j++)
                {
                    if(clue[i][j].Equals("WW"))
                    {
                        List<string> elements = new List<string>();
                        elements.Add(solution[i][j][0] + "");
                        elements.Add(solution[i][j][1] + "");
                        foreach (char c in letters.Replace(solution[i][j][0] + "", ""))
                            elements.Add("-" + c);
                        foreach (char c in numbers.Replace(solution[i][j][1] + "", ""))
                            elements.Add("-" + c);
                        foreach(string element in elements)
                        {
                            string[][] temp = copyArray(clue);
                            temp[i][j] = element.ToUpperInvariant();
                            choices.Add(temp);
                        }
                    }
                }
            }
            choices.Shuffle();
            foreach(string[][] choice in choices)
            {
                if (isDistinct(possible, choice))
                    return choice;
            }
            List<int[]> pos = new List<int[]>();
            for (int i = 0; i < gridSize; i++)
            {
                for (int j = 0; j < gridSize; j++)
                {
                    if (clue[i][j].Equals("KK"))
                    {
                        if (i > 0 && !(clue[i - 1][j].Equals("KK")))
                            pos.Add(new int[] { i, j });
                        else if (j > 0 && !(clue[i][j - 1].Equals("KK")))
                            pos.Add(new int[] { i, j });
                        else if (i < (gridSize - 1) && !(clue[i + 1][j].Equals("KK")))
                            pos.Add(new int[] { i, j });
                        else if (j < (gridSize - 1) && !(clue[i][j + 1].Equals("KK")))
                            pos.Add(new int[] { i, j });
                    }
                }
            }
            pos.Shuffle();
            clue[pos[0][0]][pos[0][1]] = "WW";
            int[] min = getMinArea(clue);
            for (int i = min[0]; i <= min[2]; i++)
            {
                for (int j = min[3]; j <= min[1]; j++)
                {
                    if (clue[i][j].Equals("KK"))
                        clue[i][j] = "WW";
                }
            }
            flag = isDistinct(possible, clue);
        }
        return clue;
    }

    // Checks if the clue can be placed exactly once
    private bool isDistinct(List<List<List<string>>> possible, string[][] clue)
    {
        string[][] temp = shrinkClue(clue);
        int sum = 0;
        for (int i = 0; i < (gridSize - temp.Length + 1); i++)
        {
            for (int j = 0; j < (gridSize - temp[0].Length + 1); j++)
            {
                if (canBePlaced(possible, temp, i, j))
                    sum++;
            }
        }
        return (sum == 1);
    }
    // Checks if the clue can be placed onto the grid at that spot
    private bool canBePlaced(List<List<List<string>>> possible, string[][] clue, int row, int col)
    {
        for (int i = 0; i < clue.Length; i++)
        {
            for (int j = 0; j < clue[i].Length; j++)
            {
                if (!(clue[i][j].Equals("WW") || clue[i][j].Equals("KK")))
                {
                    bool flag = true;
                    if (clue[i][j][0] == '-')
                    {
                        foreach(string str in possible[i + row][j + col])
                        {
                            if (!(str.Contains(clue[i][j].Substring(1))))
                            {
                                flag = false;
                                break;
                            }
                        }
                    }
                    else
                    {
                        foreach(string str in possible[i + row][j + col])
                        {
                            if (str.Contains(clue[i][j]))
                            {
                                flag = false;
                                break;
                            }
                        }
                    }
                    if (flag)
                        return false;
                }
            }
        }
        return true;
    }

    // Shrinks the clue, removing any KK spaces around it
    private string[][] shrinkClue(string[][] clue)
    {
        int[] min = getMinArea(clue);
        string[][] shrunk = new string[min[2] - min[0] + 1][];
        for (int i = 0; i < shrunk.Length; i++)
        {
            shrunk[i] = new string[min[1] - min[3] + 1];
            for (int j = 0; j < shrunk[i].Length; j++)
                shrunk[i][j] = clue[min[0] + i][min[3] + j].ToUpperInvariant();
        }
        return shrunk;
    }

    // Returns the minimal area required for the clue
    private int[] getMinArea(string[][] clue)
    {
        int top = -1, right = -1, bottom = -1, left = -1;
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                if (!(clue[i][j].Equals("KK")))
                {
                    top = i;
                    break;
                }
            }
            if (top > -1)
                break;
        }
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                if (!(clue[j][i].Equals("KK")))
                {
                    left = i;
                    break;
                }
            }
            if (left > -1)
                break;
        }
        for (int i = gridSize - 1; i >= 0; i--)
        {
            for (int j = gridSize - 1; j >= 0; j--)
            {
                if (!(clue[i][j].Equals("KK")))
                {
                    bottom = i;
                    break;
                }
            }
            if (bottom > -1)
                break;
        }
        for (int i = gridSize - 1; i >= 0; i--)
        {
            for (int j = gridSize - 1; j >= 0; j--)
            {
                if (!(clue[j][i].Equals("KK")))
                {
                    right = i;
                    break;
                }
            }
            if (right > -1)
                break;
        }
        return new int[] { top, right, bottom, left };
    }

    // Combines all the clues in the list if it can
    private void combineClues(List<string[][]> clues, string[][] solution)
    {
        clues.Shuffle();
        for (int i = 0; i < clues.Count; i++)
        {
            for (int j = i + 1; j < clues.Count; j++)
            {
                if (canCombine(clues[i], clues[j]))
                {
                    for (int row = 0; row < gridSize; row++)
                    {
                        for (int col = 0; col < gridSize; col++)
                        {
                            if (clues[i][row][col].Equals("WW"))
                                clues[i][row][col] = clues[j][row][col];
                            else if (!(clues[j][row][col].Equals("WW") || clues[j][row][col].Equals("KK")))
                            {
                                if (clues[i][row][col][0] == '-')
                                {
                                    if (!(clues[i][row][col].Substring(1).Equals(clues[j][row][col].Substring(1))))
                                        clues[i][row][col] = solution[row][col][getType(clues[i][row][col].Substring(1))] + "";
                                }
                                else if (clues[j][row][col].Length > clues[i][row][col].Length)
                                    clues[i][row][col] = clues[j][row][col];
                                else if (clues[i][row][col].Length == 1 && clues[j][row][col].Length == 1)
                                {
                                    if (getType(clues[i][row][col]) != getType(clues[j][row][col]))
                                        clues[i][row][col] = solution[row][col];
                                }
                            }
                        }
                    }
                    clues.RemoveAt(j--);
                    needUpdate = true;
                }
            }
        }
    }

    // Checks if the 2 clues can combine into 1 clue
    private bool canCombine(string[][] c1, string[][] c2)
    {
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                if (c1[i][j].Equals("KK") && !(c2[i][j].Equals("KK")))
                    return false;
                else if (c2[i][j].Equals("KK") && !(c1[i][j].Equals("KK")))
                    return false;
                else if (!(c1[i][j].Equals("KK")) && !(c1[i][j].Equals("WW")) && !(c2[i][j].Equals("KK")) && !(c2[i][j].Equals("WW")))
                {
                    if (c1[i][j][0] == '-' && c2[i][j][0] != '-')
                        return false;
                    else if (c1[i][j][0] != '-' && c2[i][j][0] == '-')
                        return false;
                    else if (c1[i][j][0] == '-' && c2[i][j][0] == '-')
                    {
                        if (getType(c1[i][j].Substring(1)) != getType(c2[i][j].Substring(1)))
                            return false;
                    }
                }
            }
        }
        return true;
    }
    private void combineNegativeClues(List<string[][]> clues)
    {
        clues.Shuffle();
        for (int i = 0; i < clues.Count; i++)
        {
            for (int j = i + 1; j < clues.Count; j++)
            {
                if (canCombineNegatives(clues[i], clues[j]))
                {
                    for (int row = 0; row < clues[i].Length; row++)
                    {
                        for (int col = 0; col < clues[i][row].Length; col++)
                        {
                            if (!clues[i][row][col].Equals("WW") && clues[i][row][col][0] != '-' && !clues[i][row][col].Equals(clues[j][row][col]))
                            {
                                int type = getType(clues[i][row][col]);
                                if (type == 0)
                                    clues[i][row][col] = "-" + letters.Replace(clues[i][row][col], "").Replace(clues[j][row][col], "");
                                else if (type == 1)
                                    clues[i][row][col] = "-" + numbers.Replace(clues[i][row][col], "").Replace(clues[j][row][col], "");
                            }
                        }
                    }
                    clues.RemoveAt(j--);
                    needUpdate = true;
                }
            }
        }
    }
    // Checks if the 2 negative clues can combine into 1 negative clue
    private bool canCombineNegatives(string[][] c1, string[][] c2)
    {
        if(c1.Length == c2.Length && c1[0].Length == c2[0].Length)
        {
            for (int i = 0; i < c1.Length; i++)
            {
                for (int j = 0; j < c1[i].Length; j++)
                {
                    if (c1[i][j].Equals("WW") && !c2[i][j].Equals("WW"))
                        return false;
                    if (!c1[i][j].Equals("WW") && c2[i][j].Equals("WW"))
                        return false;
                    if (!c1[i][j].Equals("WW") && !c2[i][j].Equals("WW"))
                    {
                        if (c1[i][j][0] == '-' && c2[i][j][0] != '-')
                            return false;
                        if (c1[i][j][0] != '-' && c2[i][j][0] == '-')
                            return false;
                        if(c1[i][j][0] != '-' && c2[i][j][0] != '-')
                        {
                            if(!c1[i][j].Equals(c2[i][j]))
                            {
                                int t1 = getType(c1[i][j]), t2 = getType(c2[i][j]);
                                if (t1 == 2 || t2 == 2 || t1 != t2)
                                    return false;
                            }
                        }
                        else if (getType(c1[i][j].Substring(1)) != getType(c2[i][j].Substring(1)))
                            return false;
                    }
                }
            }
            return true;
        }
        return false;
    }
    // Removes any clues that are not needed for the Puzzle
    private void removeRedundantClues(List<string[][]> clues, List<string[][]> negativeClues)
    {
        clues.Shuffle();
        for (int i = 0; i < clues.Count; i++)
        {
            string[][] removed = copyArray(clues[i]);
            clues.RemoveAt(i);
            if (!(canSolve(clues, negativeClues)))
                clues.Insert(i, removed);
            else
            {
                i--;
                needUpdate = true;
            }
        }
        negativeClues.Shuffle();
        for (int i = 0; i < negativeClues.Count; i++)
        {
            string[][] removed = copyArray(negativeClues[i]);
            negativeClues.RemoveAt(i);
            if (!(canSolve(clues, negativeClues)))
                negativeClues.Insert(i, removed);
            else
            {
                i--;
                needUpdate = true;
            }
        }
    }
    // Checks if the puzzle can be solved using the list of clues it is provided
    private bool canSolve(List<string[][]> clues, List<string[][]> negativeClues)
    {
        List<List<List<string>>> possible = getInitialPossible();
        List<string[][]> used = new List<string[][]>();
        bool flag = true;
        while (flag)
        {
            flag = false;
            foreach(string[][] clue in clues)
            {
                if (!(used.Contains(clue)) && isDistinct(possible, clue))
                {
                    used.Add(clue);
                    removePossibleCombinations(possible, clue);
                    removePossibleCombinations(possible);
                    flag = true;
                    break;
                }
            }
            foreach(string[][] clue in negativeClues)
            {
                if (removePossibleCombinationsNegative(possible, clue))
                {
                    removePossibleCombinations(possible);
                    flag = true;
                }
            }
        }
        foreach(List<List<string>> row in possible)
        {
            foreach(List<string> col in row)
            {
                if (col.Count > 1)
                    return false;
            }
        }
        return true;
    }
    // Checks if the puzzle can be solved using the list of clues it is provided
    private bool canTestSolve(List<string[][]> clues, List<string[][]> negativeClues)
    {
        List<List<List<string>>> possible = getInitialPossible();
        List<string[][]> used = new List<string[][]>();
        bool flag = true;
        while (flag)
        {
            flag = false;
            foreach (string[][] clue in clues)
            {
                if (!(used.Contains(clue)) && isDistinct(possible, clue))
                {
                    flag = true;
                    used.Add(clue);
                    removePossibleCombinations(possible, clue);
                    removePossibleCombinationsTest(possible);
                    printClue(clue);
                    foreach (List<List<string>> poss in possible)
                    {
                        foreach (List<string> p in poss)
                        {
                            string str = "";
                            foreach (string s in p)
                                str += s + " ";
                            Debug.LogFormat("{0}", str);
                        }
                    }
                }
            }
            foreach (string[][] clue in negativeClues)
            {
                if (removePossibleCombinationsNegative(possible, clue))
                {
                    flag = true;
                    removePossibleCombinationsTest(possible);
                    printClue(clue);
                    foreach (List<List<string>> poss in possible)
                    {
                        foreach (List<string> p in poss)
                        {
                            string str = "";
                            foreach (string s in p)
                                str += s + " ";
                            Debug.LogFormat("{0}", str);
                        }
                    }
                }
            }
            
        }
        foreach (List<List<string>> row in possible)
        {
            foreach (List<string> col in row)
            {
                if (col.Count > 1)
                    return false;
            }
        }
        return true;
    }
    // Removes redundant Clue Elements from each clue
    private void removeRedundantClueElements(List<string[][]> clues, List<string[][]> negativeClues)
    {
        clues.Shuffle();
        for (int i = 0; i < clues.Count; i++)
        {
            List<int[]> posList = new List<int[]>();
            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    if (!(clues[i][row][col].Equals("KK") || clues[i][row][col].Equals("WW")))
                        posList.Add(new int[] { row, col });
                }
            }
            posList.Shuffle();
            foreach(int[] pos in posList)
            {
                string[][] temp = copyArray(clues[i]);
                clues[i][pos[0]][pos[1]] = "WW";
                if (!(canSolve(clues, negativeClues)))
                {
                    if (temp[pos[0]][pos[1]][0] != '-' && temp[pos[0]][pos[1]].Length == 2)
                    {
                        List<char> choices = new List<char>();
                        choices.Add(temp[pos[0]][pos[1]][0]);
                        choices.Add(temp[pos[0]][pos[1]][1]);
                        choices.Shuffle();
                        bool flag = true;
                        foreach (char c in choices)
                        {
                            clues[i][pos[0]][pos[1]] = c + "";
                            if (canSolve(clues, negativeClues))
                            {
                                flag = false;
                                break;
                            }
                        }
                        if (flag)
                            clues[i] = temp;
                        else
                            needUpdate = true;
                    }
                    else
                        clues[i] = temp;
                }
                else
                    needUpdate = true;
            }
        }
    }
    // Turns 3x3 negative clues into positive clues
    private bool negativesToPositives(List<string[][]> clues, List<string[][]> negativeClues, string[][] solution)
    {
        bool updated = false;
        for(int i = 0; i < negativeClues.Count; i++)
        {
            if(negativeClues[i].Length == gridSize && negativeClues[i][0].Length == gridSize)
            {
                string[][] temp = copyArray(negativeClues[i]);
                negativeClues.RemoveAt(i);
                List<int[]> elementPositions = getElementPositions(temp);
                foreach(int[] p in elementPositions)
                {
                    string element = temp[p[0]][p[1]];
                    if(element[0] == '-')
                        temp[p[0]][p[1]] = solution[p[0]][p[1]][getType(element.Substring(1))] + "";
                    else
                    {
                        foreach (char c in solution[p[0]][p[1]])
                            element = element.Replace(c + "", "");
                        if(element.Length > 0)
                            temp[p[0]][p[1]] = "-" + element[Random.Range(0, element.Length)];
                    }
                }
                clues.Add(temp);
                i--;
                updated = true;
            }
        }
        return updated;
    }
    // Removes redundant spaces from each clue
    private void removeRedundantSpaces(List<string[][]> clues, List<string[][]> negativeClues)
    {
        clues.Shuffle();
        List<int> types = new List<int>();
        for (int i = 0; i < 4; i++)
            types.Add(i);
        types.Shuffle();
        for (int i = 0; i < clues.Count; i++)
        {
            for(int j = 0; j < types.Count; j++)
            {
                bool flag = true;
                int[] minArea = getMinArea(clues[i]);  // Top, Right, Bottom, Left
                if (types[j] % 2 == 0)
                {
                    for (int k = 0; k < gridSize; k++)
                    {
                        if (!(clues[i][minArea[types[j]]][k].Equals("WW") || clues[i][minArea[types[j]]][k].Equals("KK")))
                        {
                            flag = false;
                            break;
                        }
                    }
                }
                else
                {
                    for (int k = 0; k < gridSize; k++)
                    {
                        if (!(clues[i][k][minArea[types[j]]].Equals("WW") || clues[i][k][minArea[types[j]]].Equals("KK")))
                        {
                            flag = false;
                            break;
                        }
                    }
                }
                if (flag)
                {
                    string[][] temp = copyArray(clues[i]);
                    if (types[j] % 2 == 0)
                    {
                        for (int k = 0; k < gridSize; k++)
                            clues[i][minArea[types[j]]][k] = "KK";
                    }
                    else
                    {
                        for (int k = 0; k < gridSize; k++)
                            clues[i][k][minArea[types[j]]] = "KK";
                    }
                    if (!(canSolve(clues, negativeClues)))
                        clues[i] = temp;
                    else
                    {
                        needUpdate = true;
                        j--;
                    }
                        
                }
            }
        }
        types.Shuffle();
        for(int i = 0; i < negativeClues.Count; i++)
        {
            for(int j = 0; j < types.Count; j++)
            {
                bool flag;
                if (types[j] % 2 == 0)
                    flag = negativeClues[i].Length < 3;
                else
                    flag = negativeClues[i][0].Length < 3;
                if(flag)
                {
                    string[][] temp = types[j] % 2 == 0 ? getBlankClue(negativeClues[i].Length + 1, negativeClues[i][0].Length) : getBlankClue(negativeClues[i].Length, negativeClues[i][0].Length + 1);
                    int offset = types[j] / 2 == 0 ? 0 : 1;
                    if(types[j] % 2 == 0)
                    {
                        for (int row = 0; row < negativeClues[i].Length; row++)
                        {
                            for (int col = 0; col < negativeClues[i][row].Length; col++)
                                temp[row + offset][col] = negativeClues[i][row][col];
                        }
                    }
                    else
                    {
                        for (int row = 0; row < negativeClues[i].Length; row++)
                        {
                            for (int col = 0; col < negativeClues[i][row].Length; col++)
                                temp[row][col + offset] = negativeClues[i][row][col];
                        }
                    }
                    string[][] removed = copyArray(negativeClues[i]);
                    negativeClues[i] = temp;
                    if (!(canSolve(clues, negativeClues)))
                        negativeClues[i] = removed;
                    else
                    {
                        needUpdate = true;
                        j--;
                    }
                }
            }
        }
    }

    // Randomly turns white spaces into black spaces
    private void turnSpacesBlack(List<string[][]> clues)
    {
        for(int i = 0; i < clues.Count; i++)
        {
            List<int[]> positions = new List<int[]>();
            for(int row = 0; row < clues[i].Length; row++)
            {
                for(int col = 0; col < clues[i][row].Length; col++)
                {
                    if (clues[i][row][col].Equals("WW") && canTurnBlack(clues[i], row, col))
                        positions.Add(new int[] { row, col });
                }
            }
            positions.Shuffle();
            for(int j = 0; j < positions.Count; j++)
            {
                if (canTurnBlack(clues[i], positions[j][0], positions[j][1]) && UnityEngine.Random.Range(0, 2) == 0)
                    clues[i][positions[j][0]][positions[j][1]] = "KK";
            }
        }
    }
    //This checks if the space can be turned black.
    private bool canTurnBlack(string[][] clue, int row, int col)
    {
        string[][] temp = new string[gridSize][];
        for (int i = 0; i < gridSize; i++)
        {
            temp[i] = new string[gridSize];
            for (int j = 0; j < gridSize; j++)
            {
                if (i < clue.Length && j < clue[i].Length)
                    temp[i][j] = clue[i][j] + "";
                else
                    temp[i][j] = "KK";
            }
        }
        //Checks if they are the same size when turning that space black
        temp[row][col] = "KK";
        temp = shrinkClue(temp);
        if (temp.Length == clue.Length && temp[0].Length == clue[0].Length)
        {
            //Checking if the spaces are connected
            row = -1; col = -1;
            for (int i = 0; i < temp.Length; i++)
            {
                for (int j = 0; j < temp[i].Length; j++)
                {
                    if (!(temp[i][j].Equals("KK")))
                    {
                        row = i;
                        col = j;
                        temp[i][j] = "KK";
                        break;
                    }
                }
                if (row >= 0)
                    break;
            }
            string dir = "";
            while (true)
            {
                if (row > 0 && !(temp[row - 1][col].Equals("KK")))
                {
                    temp[--row][col] = "KK";
                    dir += "U";
                }
                else if (row < (temp.Length - 1) && !(temp[row + 1][col].Equals("KK")))
                {
                    temp[++row][col] = "KK";
                    dir += "D";
                }
                else if (col > 0 && !(temp[row][col - 1].Equals("KK")))
                {
                    temp[row][--col] = "KK";
                    dir += "L";
                }
                else if (col < (temp[row].Length - 1) && !(temp[row][col + 1].Equals("KK")))
                {
                    temp[row][++col] = "KK";
                    dir += "R";
                }
                else
                {
                    if (dir.Length == 0)
                        break;
                    switch (dir[dir.Length - 1])
                    {
                        case 'U':
                            row++;
                            break;
                        case 'D':
                            row--;
                            break;
                        case 'L':
                            col++;
                            break;
                        case 'R':
                            col--;
                            break;
                    }
                    dir = dir.Substring(0, dir.Length - 1);
                }
            }
            for (int i = 0; i < temp.Length; i++)
            {
                for (int j = 0; j < temp[i].Length; j++)
                {
                    if (!(temp[i][j].Equals("KK")))
                        return false;
                }
            }
            return true;
        }
        return false;
    }
    //Returns a negative clue
    private string[][] getNegativeClue(string[][] solution, List<List<List<string>>> possible, int row, int col)
    {
        List<string> clueElements = new List<string>();
        clueElements.Add(solution[row][col][0] + "");
        clueElements.Add(solution[row][col][1] + "");
        //Placing a negative on a negative makes it a positive clue
        //While I'm not against it, someone suggest that I would remove this part
        /*
        string negatives = letters + numbers;
        negatives = negatives.Replace(solution[row][col][0] + "", "").Replace(solution[row][col][1] + "", "");
        foreach (char negative in negatives)
            clueElements.Add("-" + negative);
        */
        clueElements.Shuffle();
        clueElements.Add(solution[row][col]);
        foreach (string clueElement in clueElements)
        {
            string[][] clue = getBlankNegativeClue(solution, possible, row, col);
            if (clue == null)
                return null;
            clue = getNegativeClueElementPlacement(solution, possible, row, col, clueElement, clue);
            if (clue != null)
            {
                if (isNegativeClueContradicting(solution, clue))
                {
                    List<int> possSpaces = new List<int>();
                    for (int i = 0; i < clue.Length; i++)
                    {
                        for (int j = 0; j < clue[i].Length; j++)
                        {
                            if (clue[i][j].Equals("WW"))
                                possSpaces.Add(i * gridSize + j);
                        }
                    }
                    possSpaces.Shuffle();
                    List<string> possElements = new List<string>();
                    foreach (char letter in letters)
                    {
                        possElements.Add("" + letter);
                        possElements.Add("-" + letter);
                    }
                    foreach (char number in numbers)
                    {
                        possElements.Add("" + number);
                        possElements.Add("-" + number);
                    }
                    foreach (int possSpace in possSpaces)
                    {
                        int r = possSpace / 3;
                        int c = possSpace % 3;
                        possElements.Shuffle();
                        foreach (string element in possElements)
                        {
                            string[][] possibleClue = copyArray(clue);
                            possibleClue[r][c] = element;
                            if (validNegativeClue(solution, possible, row, col, possibleClue) && !isNegativeClueContradicting(solution, possibleClue))
                                return possibleClue;
                        }
                    }
                }
                else
                    return clue;
            }
        }
        return null;
    }
    //Returns a Blank Negative Clue, which is an area where the clue element is NOT placed in
    //Note that the size of the clue it returns can be less than 3x3
    private string[][] getBlankNegativeClue(string[][] solution, List<List<List<string>>> possible, int row, int col)
    {
        string[][] clue = getBlankClue();
        for(int i = 0; i < gridSize; i++)
        {
            for(int j = 0; j < gridSize; j++)
            {
                if (possible[i][j].Contains(solution[row][col]))
                    clue[i][j] = "KK";
                clue[i][j] = clue[i][j].Equals("WW") ? "KK" : "WW";
            }
        }
        clue[row][col] = "KK";
        bool flag = true;
        for(int i = 0; i < clue.Length; i++)
        {
            for(int j = 0; j < clue[i].Length; j++)
            {
                if(clue[i][j].Equals("WW"))
                    flag = false;
            }
        }
        if (flag)
            return null;
        return invertClue(shrinkClue(clue));
    }
    //Returns an inverted clue used for negative clue creation
    private string[][] invertClue(string[][] clue)
    {
        int newRow = (gridSize + 1) - clue.Length, newCol = (gridSize + 1) - clue[0].Length;
        string[][] newClue = new string[newRow][];
        for(int i = 0; i < newRow; i++)
        {
            newClue[i] = new string[newCol];
            for (int j = 0; j < newCol; j++)
                newClue[i][j] = "WW";
        }
        return newClue;
    }
    //Returns the placement of the element on a Negative Clue
    private string[][] getNegativeClueElementPlacement(string[][] solution, List<List<List<string>>> possible, int row, int col, string clueElement, string[][] clue)
    {
        bool flag = false;
        for (int i = 0; i < clue.Length; i++)
        {
            for (int j = 0; j < clue[i].Length; j++)
            {
                string[][] possibleClue = copyArray(clue);
                possibleClue[i][j] = clueElement;
                if(validNegativeClue(solution, possible, row, col, possibleClue))
                {
                    clue = possibleClue;
                    flag = true;
                    goto exitLoop;
                }
            }
        }
    exitLoop:
        if (flag)
            return clue;
        return null;
    }
    // Checks if the Negative Clue removes all possible placement of the combo in the current grid
    private bool validNegativeClue(string[][] solution, List<List<List<string>>> possible, int row, int col, string[][] clue)
    {
        string combo = solution[row][col];
        List<int> positions = new List<int>(); 
        for(int i = 0; i < gridSize; i++)
        {
            for(int j = 0; j < gridSize; j++)
            {
                if (possible[i][j].Contains(combo))
                    positions.Add((i * gridSize) + j);
            }
        }
        positions.RemoveAt(positions.IndexOf((row * gridSize) + col));
        List<int[]> elementPositions = getElementPositions(clue);
        if (elementPositions.Count == 1)
        {
            for (int i = 0; i < (gridSize - clue.Length + 1); i++)
            {
                for (int j = 0; j < (gridSize - clue[0].Length + 1); j++)
                {
                    if (canBePlaced(possible, clue, i, j))
                        positions.Remove(((i + elementPositions[0][0]) * gridSize) + j + elementPositions[0][1]);
                }
            }
            return (positions.Count == 0);
        }
        else
        {
            for (int i = 0; i < (gridSize - clue.Length + 1); i++)
            {
                for (int j = 0; j < (gridSize - clue[0].Length + 1); j++)
                {
                    if (canBePlaced(possible, clue, i, j))
                    {
                        if(onlyContainsElement(possible, clue[elementPositions[0][0]][elementPositions[0][1]], i + elementPositions[0][0], j + elementPositions[0][1]))
                            positions.Remove(((i + elementPositions[1][0]) * gridSize) + j + elementPositions[1][1]);
                        else if(onlyContainsElement(possible, clue[elementPositions[1][0]][elementPositions[1][1]], i + elementPositions[1][0], j + elementPositions[1][1]))
                            positions.Remove(((i + elementPositions[0][0]) * gridSize) + j + elementPositions[0][1]);    
                    }
                }
            }
            return (positions.Count == 0);
        }
    }
    //Checks to see if the current grid only contains that element at a specific space
    private bool onlyContainsElement(List<List<List<string>>> possible, string element, int row, int col)
    {
        if(element[0] == '-')
        {
            foreach (string poss in possible[row][col])
            {
                if (poss.Contains(element.Substring(1)))
                    return false;
            }
        }
        else
        {
            foreach (string poss in possible[row][col])
            {
                if (!poss.Contains(element))
                    return false;
            }
        }
        return true;
    }
    //Checks to see if the Negative Clue being used contradicts with the solution
    private bool isNegativeClueContradicting(string[][] solution, string[][] clue)
    {
        for (int i = 0; i <= (gridSize - clue.Length); i++)
        {
            for (int j = 0; j <= (gridSize - clue[0].Length); j++)
            {
                if (canBePlaced(solution, clue, i, j))
                    return true;
            }
        }
        return false;
    }
    //Checks to see if the clue can be placed in that area of the solution
    private bool canBePlaced(string[][] solution, string[][] clue, int row, int col)
    {
        for (int i = 0; i < clue.Length; i++)
        {
            for (int j = 0; j < clue[i].Length; j++)
            {
                if (!(clue[i][j].Equals("WW") || clue[i][j].Equals("KK")))
                {
                    bool flag = true;
                    if (clue[i][j][0] == '-')
                    {
                        if (!(solution[i + row][j + col].Contains(clue[i][j].Substring(1))))
                            flag = false;
                    }
                    else if (solution[i + row][j + col].Contains(clue[i][j]))
                        flag = false;
                    if (flag)
                        return false;
                }
            }
        }
        return true;
    }
    // Removes possible combinations using the given clue
    private bool removePossibleCombinationsNegative(List<List<List<string>>> possible, string[][] clue)
    {
        bool f = false;
        for (int row = 0; row <= (gridSize - clue.Length); row++)
        {
            for (int col = 0; col <= (gridSize - clue[0].Length); col++)
            {
                if(negativeCanBePlaced(possible, clue, row, col))
                {
                    List<int[]> elementPositions = getElementPositions(clue);
                    if(elementPositions.Count == 1)
                        f = removeNegativeElements(clue[elementPositions[0][0]][elementPositions[0][1]], possible, elementPositions[0][0] + row, elementPositions[0][1] + col) || f;
                    else if (onlyContainsElement(possible, clue[elementPositions[0][0]][elementPositions[0][1]], row + elementPositions[0][0], col + elementPositions[0][1]))
                        f = removeNegativeElements(clue[elementPositions[1][0]][elementPositions[1][1]], possible, elementPositions[1][0] + row, elementPositions[1][1] + col) || f;
                    else if (onlyContainsElement(possible, clue[elementPositions[1][0]][elementPositions[1][1]], row + elementPositions[1][0], col + elementPositions[1][1]))
                        f = removeNegativeElements(clue[elementPositions[0][0]][elementPositions[0][1]], possible, elementPositions[0][0] + row, elementPositions[0][1] + col) || f;
                }
            }
        }
        return f;
    }
    // Checks if the clue can be placed onto the grid at that spot
    private bool negativeCanBePlaced(List<List<List<string>>> possible, string[][] clue, int row, int col)
    {
        List<int[]> elementPositions = getElementPositions(clue);
        if (elementPositions.Count == 1)
            return canBePlaced(possible, clue, row, col);
        else if (canBePlaced(possible, clue, row, col))
        {
            if (onlyContainsElement(possible, clue[elementPositions[0][0]][elementPositions[0][1]], row + elementPositions[0][0], col + elementPositions[0][1]))
                return true;
            else if (onlyContainsElement(possible, clue[elementPositions[1][0]][elementPositions[1][1]], row + elementPositions[1][0], col + elementPositions[1][1]))
                return true;
        }
        return false;
    }
    //Removes elements according to the element in a negative clue
    private bool removeNegativeElements(string element, List<List<List<string>>> possible, int row, int col)
    {
        bool flag = false;
        if (element[0] == '-')
        {
            for(int index = 0; index < possible[row][col].Count; index++)
            {
                if(!possible[row][col][index].Contains(element.Substring(1)))
                {
                    possible[row][col].RemoveAt(index);
                    flag = true;
                    index--;
                }
            }
        }
        else
        {
            for (int index = 0; index < possible[row][col].Count; index++)
            {
                if (possible[row][col][index].Contains(element))
                {
                    possible[row][col].RemoveAt(index);
                    flag = true;
                    index--;
                }
            }
        }
        return flag;
    }
    // Removes clues that contain 0 clue elements
    private void removeEmptyClues(List<string[][]> clues)
    {
        for(int i = 0; i < clues.Count; i++)
        {
            List<int[]> ep = getElementPositions(clues[i]);
            if (ep.Count == 0)
                clues.RemoveAt(i--);
        }
    }
    // Copies the array
    private string[][] copyArray(string[][] arr)
    {
        string[][] copy = new string[arr.Length][];
        for (int i = 0; i < arr.Length; i++)
        {
            copy[i] = new string[arr[i].Length];
            for (int j = 0; j < arr[i].Length; j++)
                copy[i][j] = arr[i][j].ToUpperInvariant();
        }
        return copy;
    }
    // Returns the clue type
    private int getType(string c)
    {
        if (letters.Contains(c))
            return 0;
        else if (numbers.Contains(c))
            return 1;
        return 2;
    }
    private void printClue(string[][] clue)
    {
        foreach(string[] row in clue)
        {
            string str = "";
            foreach (string space in row)
                str += space + " ";
            Debug.LogFormat("{0}", str);
        }
    }
    //Returns the positions of each element
    private List<int[]> getElementPositions(string[][] clue)
    {
        List<int[]> elementPositions = new List<int[]>();
        for(int i = 0; i < clue.Length; i++)
        {
            for(int j = 0; j < clue[i].Length; j++)
            {
                if (!(clue[i][j].Equals("WW") || clue[i][j].Equals("KK")))
                    elementPositions.Add(new int[] { i, j });
            }
        }
        return elementPositions;
    }

}
