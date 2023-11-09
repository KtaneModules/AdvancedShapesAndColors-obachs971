using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clue 
{
    private string[][] clue;
    private bool positive;
	public Clue(string[][] clue, bool positive)
    {
        this.clue = clue;
        this.positive = positive;
    }
    
    public string[][] getClue()
    {
        return clue;
    }
    public bool isPositive()
    {
        return positive;
    }
	
}
