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
        public void StartIntroSpray(MenuOptionData placeholder)
        {
            if(placeholder.clack)metal.PlayOneShot(placeholder.clack,placeholder.volume*.4f);
            aerosol.clip=placeholder.spray;aerosol.pitch=1;
            aerosol.volume=placeholder.volume*.55f;aerosol.loop=true;aerosol.Play();
        }
        public void StopSpray() { aerosol.Stop();aerosol.loop=false; }
        void OnDisable() { metal.Stop(); aerosol.Stop(); music.Stop(); }
    }
}
