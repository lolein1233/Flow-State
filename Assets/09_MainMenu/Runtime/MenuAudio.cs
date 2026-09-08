using UnityEngine;
namespace FlowState.Menu
{
    public sealed class MenuAudio : MonoBehaviour
    {
        public AudioSource metal, aerosol, music;
        public void Turn(MenuOptionData data)
        {
            metal.pitch=data.pitch*Random.Range(.94f,1.06f);
            if(data.rattle) metal.PlayOneShot(data.rattle,data.volume);
        }
        public void Spray(MenuOptionData data)
        {
            metal.pitch=data.pitch;
            if(data.clack)metal.PlayOneShot(data.clack,data.volume);
            aerosol.pitch=data.pitch*Random.Range(.97f,1.03f);
            aerosol.clip=data.spray; aerosol.volume=data.volume*data.pressure;
            aerosol.Play();
        }
        public void StopSpray() { aerosol.Stop(); }
        void OnDisable() { metal.Stop(); aerosol.Stop(); music.Stop(); }
    }
}
