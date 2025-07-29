package com.OUT23.sfs2x;

import com.smartfoxserver.v2.api.SFSApi;
import com.smartfoxserver.v2.entities.Room;
import com.smartfoxserver.v2.entities.User;
import com.smartfoxserver.v2.entities.Zone;
import com.smartfoxserver.v2.entities.variables.RoomVariable;
import com.smartfoxserver.v2.entities.variables.SFSRoomVariable;
import com.smartfoxserver.v2.entities.data.ISFSObject;
import com.smartfoxserver.v2.entities.data.SFSObject;
import com.smartfoxserver.v2.extensions.BaseClientRequestHandler;

import java.util.Arrays;

public class RelojHandler extends BaseClientRequestHandler {

    @Override
    public void handleClientRequest(User user, ISFSObject params) {
        try {
        	
            String roomCode = params.containsKey("roomCode") ? params.getUtfString("roomCode") : "";

            if (roomCode.isEmpty()) {
                trace("❌ roomCode no encontrado en la solicitud.");
                return;
            }
            Zone zone = user.getZone();
            
            Room room = zone.getRoomByName(roomCode);

            if (room == null) {
                trace("❌ No se encontró la sala con el código: " + roomCode);
                return;
            }
            
            String variableName = "reloj";
            int nuevoValor = params.containsKey("valor") ? params.getInt("valor") : -1;

            RoomVariable reloj = room.getVariable(variableName);

            if (reloj == null && nuevoValor >= 0) {
                reloj = new SFSRoomVariable(variableName, nuevoValor);
                reloj.setGlobal(true);
                reloj.setPersistent(true);
                reloj.setHidden(false);
                getApi().setRoomVariables(user, room, Arrays.asList(reloj));
                trace("⏰ Variable 'reloj' creada con valor: " + nuevoValor);
            }

            int valorActual = (reloj != null) ? (int) reloj.getValue() : nuevoValor;
            
            ISFSObject response = new SFSObject();
            response.putInt("reloj", valorActual);
            send("reloj_response", response, user);

        } catch (Exception e) {
            trace("❌ Error en RelojHandler: " + e.getMessage());
        }
    }
}
