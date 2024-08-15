using Sandbox;
using System;

// Classes should inherit from this interface if they are undoable with the "Z" key default
public interface IUndoable
{
  void Undo();
}


public sealed class PropToolManager : Component
{
  [Property] GameObject PropPrefab { get; set; }
  [Property] public int PropLimit { get; set; } = 10;
  ///
  /// PROPS
  /// 

  List<GameObject> Props { get; set; } = new List<GameObject>();
  public class PropAction : IUndoable
  {
    private readonly PropToolManager _propToolManager;

    public PropAction(PropToolManager propToolManager, GameObject prop)
    {
        _propToolManager = propToolManager;
        Prop = prop;
        Position = prop.Transform.Position;
        Rotation = prop.Transform.Rotation;
    }
    
    private GameObject Prop { get; }
    private Vector3 Position { get; }
    private Rotation Rotation { get; }

    public void Undo()
    {
      Prop.Destroy();
      _propToolManager.Props.Remove( Prop );
    }
  }

  /// <summary>
  /// Returns the number of props currently spawned and owned by the player
  /// </summary>
  /// <returns></returns>
  public int PropCount()
  {
    return Props.Count;
  }

  /// <summary>
  /// Removes all props from the player
  /// </summary>
  public void RemoveAllProps()
  {
    foreach ( var prop in Props )
    {
      prop.Destroy();
    }
    Props.Clear();
  }

  /// <summary>
  /// Attempts to spawn a prop at the player's forward line trace, else spawns it 50 units in front of the player
  /// </summary>
  /// <param name="modelname"></param>
  public void SpawnProp( string modelname )
  {
    if ( Props.Count >= PropLimit )
    {
      Sound.Play( "audio/error.sound" );
      return;
    }else
    {
      // TODO change this sound
      Sound.Play( "audio/select.sound" );
    }

    // spawn the prop prefab
    // TODO fix this not working
    Vector3? nullablePlayerPos = GameObject.Components.Get<PlayerInteraction>()?.ForwardLineTrace();
    Vector3 playerPos = nullablePlayerPos ?? Vector3.Zero;

    if ( playerPos == Vector3.Zero )
    {
      playerPos = GameObject.Transform.World.Position + GameObject.Transform.Local.Forward * 50;
    }
    GameObject Prop = PropPrefab.Clone( playerPos );

    // Update the prop model
    Prop.Components.Get<PropLogic>().UpdatePropModel( modelname );

    // Update the prop Collider
    Prop.Components.Get<PropLogic>().UpdatePropCollider( modelname );

    // TODO perhaps encapsulate this whole process in a try catch. If exception, attempt to clean up after itself
    // Spawn the prop on all clients
    Prop.NetworkSpawn();
    Props.Add( Prop );
    History.Add( new PropAction( this, Prop ) );
  }

  protected override void OnUpdate()
  {
    if ( Input.Pressed( "Undo" ) )
    {
      try
      {
        UndoLastAction();
      }
      catch ( Exception e )
      {
        Log.Error( e );
      }
    }
  }

  /// 
  /// HISTORY
  /// 

  List<IUndoable> History { get; set; } = new List<IUndoable>();
  public void UndoLastAction()
  {
    if ( History.Count > 0 )
    {
      // TODO change this sound
      Sound.Play( "audio/error.sound" );
      History.Last().Undo();
      History.RemoveAt( History.Count - 1 );
    }
  }
}