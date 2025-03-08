
using System;
using DI;
using UnityEngine;

public class MyClassService
{
        
}

public class MyClassScene
{
    
}

public class MyFactory
{
    public MyObject Create(string name, int index)
    {
        return new MyObject
        {
            name = name,
            key = index
        };
    }
}

public class MyObject
{
    public string name{get;set;}
    public int key{get;set;}
}


public class DIExample1 : MonoBehaviour
{
    private void Awake()
    {
        var projectContainer = new DIContainer();
        
        //only register object, and when I ask it somewhere it will create an Instance
        projectContainer.RegisterSingleton(_ => new MyClassService());
        
        //by tag, we will have 3 singletones of the same class
        projectContainer.RegisterSingleton("myFactory",_ => new MyClassScene());
        projectContainer.RegisterSingleton("myFactory2",_ => new MyClassScene());
        
        //now he will create new Instance
        var myFactory = projectContainer.Resolve<MyFactory>();
        var myFactory2 = projectContainer.Resolve<MyFactory>("myFactory");
        var myFactory3 = projectContainer.Resolve<MyFactory>("myFactory2");
    }
}