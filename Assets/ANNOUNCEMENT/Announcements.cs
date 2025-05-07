using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;

public class Announcements : MonoBehaviour
{
    public static Announcements instance;
    public GameObject announcementObject;
    
    void Awake(){
        instance = this;
    }

    void Start(){
        announcementObject.SetActive(false);
    }

    public void Announce(string atext, float atime){
        StartCoroutine(ShowAnnouncement(atext, atime));
    }

    public IEnumerator ShowAnnouncement(string atext, float atime){
        announcementObject.SetActive(true);
        announcementObject.GetComponentInChildren<TextMeshProUGUI>().text = atext;
        yield return new WaitForSeconds(atime);
        announcementObject.SetActive(false);
    }
}
