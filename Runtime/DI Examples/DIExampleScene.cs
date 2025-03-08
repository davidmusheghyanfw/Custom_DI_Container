using DI;
using UnityEngine;

namespace CDI.DI_Examples
{
    public class DIExampleScene : MonoBehaviour
    {
        [Inject]
        public DIExampleScene(DIContainer projectContainer)
        {
            
        }
    }
}