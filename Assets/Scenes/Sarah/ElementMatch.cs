using System;
using System.Collections.Generic;

[Serializable]
public class ElementMatchList
{
    public List<ElementMatch> elements;
}

[Serializable]
public class ElementMatch
{
    public string label;
    public string category;
    public List<string> time_stamps;
    public List<ElementImage> images;
}

[Serializable]
public class ElementImage
{
    public string image_name;
    public List<string> tags;
    public string link;
    public float similarity_score;
}
