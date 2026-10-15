namespace MMXOnline;

public partial class Character {
	public int lightningWebUses;
	public Collider? lightningWebSupport;
	public bool groundedOnLightningWeb;
	public ShotgunIceProjSled? shotgunIceSled;

	public bool canUseLightningWeb(Collider web) {
		return lightningWebUses < 2 || lightningWebSupport == web;
	}

	public void useLightningWeb(Collider web) {
		if (!web.isLightningWeb || lightningWebSupport == web) return;
		if (!canUseLightningWeb(web)) return;
		lightningWebUses++;
		lightningWebSupport = web;
	}

	public void resetLightningWebUses() {
		lightningWebUses = 0;
		lightningWebSupport = null;
	}
}
