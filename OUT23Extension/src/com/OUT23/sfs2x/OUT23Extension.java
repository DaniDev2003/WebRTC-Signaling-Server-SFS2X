package com.OUT23.sfs2x;

import com.smartfoxserver.v2.SmartFoxServer;
import com.smartfoxserver.v2.entities.Zone;
import com.smartfoxserver.v2.extensions.SFSExtension;

public class OUT23Extension extends SFSExtension {
    
	@Override
	public void init() {
	    trace("🔍 Zonas disponibles:");
	    for (Zone zone : SmartFoxServer.getInstance().getZoneManager().getZoneList()) {
	        trace("➡️ Zona: " + zone.getName());
	    }

	    trace("SalaExtension iniciada.");
	    addRequestHandler("crear_sala", CrearSalaHandler.class);
	    addRequestHandler("obtener_salas", ObtenerSalasHandler.class);
	    addRequestHandler("setRoomVariable", SetRoomVariableHandler.class);
	    addRequestHandler("eliminar_sala", EliminarSalaHandler.class);
	    addRequestHandler("trigger", TriggerHandler.class);
	    addRequestHandler("WebRTC", WebRTCHandler.class);
        addRequestHandler("reloj", RelojHandler.class);
        
        SmartFoxServer.getInstance().getTaskScheduler().schedule(new RelojUpdater(this), 1, java.util.concurrent.TimeUnit.SECONDS);
        trace("⏱️ RelojUpdater inicializado correctamente.");
	}
}