package com.OUT23.sfs2x;

import com.smartfoxserver.v2.SmartFoxServer;
import com.smartfoxserver.v2.entities.Room;
import com.smartfoxserver.v2.entities.Zone;
import com.smartfoxserver.v2.entities.variables.RoomVariable;
import com.smartfoxserver.v2.entities.variables.SFSRoomVariable;
import com.smartfoxserver.v2.extensions.ISFSExtension;
import com.smartfoxserver.v2.extensions.BaseSFSExtension;

import java.util.Arrays;
import java.util.List;

public class RelojUpdater implements Runnable {
    private final BaseSFSExtension extension;

    public RelojUpdater(BaseSFSExtension extension) {
        this.extension = extension;
    }

    @Override
    public void run() {
        try {
            Zone zone = SmartFoxServer.getInstance().getZoneManager().getZoneByName("OUT23");
            List<Room> salas = zone.getRoomList();

            for (Room sala : salas) {
                RoomVariable relojVar = sala.getVariable("reloj");

                if (relojVar != null && relojVar.getValue() instanceof Integer) {
                    int valor = (int) relojVar.getValue();

                    if (valor > 0) {
                        int nuevoValor = valor - 1;
                        RoomVariable nuevaVar = new SFSRoomVariable("reloj", nuevoValor);
                        nuevaVar.setGlobal(true);
                        nuevaVar.setPersistent(true);
                        nuevaVar.setHidden(false);

                        extension.getApi().setRoomVariables(null, sala, Arrays.asList(nuevaVar));
                    }
                }
            }

        } catch (Exception e) {
            extension.trace("❌ Error en RelojUpdater: " + e.getMessage());
        } finally {
            // Reagendar la tarea cada 1 segundo
            SmartFoxServer.getInstance().getTaskScheduler().schedule(this, 1, java.util.concurrent.TimeUnit.SECONDS);
        }
    }
}
