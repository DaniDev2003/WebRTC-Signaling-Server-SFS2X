package com.OUT23.sfs2x;

import java.util.ArrayList;
import java.util.Random;

import com.smartfoxserver.v2.api.CreateRoomSettings;
import com.smartfoxserver.v2.entities.SFSRoomRemoveMode;
import com.smartfoxserver.v2.entities.User;
import com.smartfoxserver.v2.entities.Zone;
import com.smartfoxserver.v2.entities.data.ISFSObject;
import com.smartfoxserver.v2.entities.data.SFSObject;
import com.smartfoxserver.v2.entities.variables.RoomVariable;
import com.smartfoxserver.v2.entities.variables.SFSRoomVariable;
import com.smartfoxserver.v2.extensions.BaseClientRequestHandler;

public class CrearSalaHandler extends BaseClientRequestHandler {

    private final String CLAVE_ESPERADA = "out23business";

    @Override
    public void handleClientRequest(User user, ISFSObject params) {
        try {
            trace("👉 Petición crear_sala recibida de " + user.getName());

            // Obtenemos valores con validación
            String clave = params.containsKey("clave") ? params.getUtfString("clave") : "";
            String tipo = params.containsKey("type") ? params.getUtfString("type") : "general";
            String nombre = params.containsKey("nombre") ? params.getUtfString("nombre") : "sin_nombre";
            Integer maxUsers = params.containsKey("maxusers") ? params.getInt("maxusers") : 4;

            // Validación de clave
            if (!CLAVE_ESPERADA.equals(clave)) {
                ISFSObject response = new SFSObject();
                response.putUtfString("error", "Clave incorrecta.");
                send("crear_sala", response, user);
                return;
            }

            // Validación de maxUsers
            if (maxUsers <= 0) {
                trace("❗ MaxUsers inválido. Usando valor por defecto: 4");
                maxUsers = 4;
            }

            trace("📋 Datos - Tipo: " + tipo + ", Nombre: " + nombre + ", MaxUsers: " + maxUsers);

            // Generar código y verificar
            String codigo = generarCodigo();
            if (codigo == null || codigo.isEmpty()) {
                throw new Exception("Código generado es inválido.");
            }

            String roomName = codigo;

            CreateRoomSettings settings = new CreateRoomSettings();
            settings.setName(roomName);
            settings.setMaxUsers(maxUsers);
            settings.setMaxVariablesAllowed(55); // o el número que necesites
            settings.setDynamic(false);
            settings.setGroupId("default");
            settings.setGame(true);
            settings.setHidden(false);
            settings.setAutoRemoveMode(SFSRoomRemoveMode.NEVER_REMOVE);

            // Inicialización de RoomVariables
            if (settings.getRoomVariables() == null) {
                trace("Inicializando lista de variables de sala...");
                settings.setRoomVariables(new ArrayList<>());
            }

            // Creación de variables con trazas
            RoomVariable codigoVar = new SFSRoomVariable("codigo", codigo);
            RoomVariable tipoVar = new SFSRoomVariable("tipo", tipo);
            RoomVariable nombreVar = new SFSRoomVariable("nombre", nombre);

            settings.getRoomVariables().add(codigoVar);
            settings.getRoomVariables().add(tipoVar);
            settings.getRoomVariables().add(nombreVar);
            
            int[] orden = {0, 1, 2, 3, 4, 5, 10, 6, 7, 8, 9};
            for (int i : orden) {
                RoomVariable enigmaVar = new SFSRoomVariable("enigm" + i, false);
                enigmaVar.setGlobal(true);       
                enigmaVar.setPersistent(true);    
                enigmaVar.setHidden(false);
                settings.getRoomVariables().add(enigmaVar);
            }

            Zone currentZone = getParentExtension().getParentZone();

            trace("🏗 Creando sala con código: " + codigo);

            // Creación de sala con manejo de excepción
            try {
                currentZone.getRoomManager().createRoom(settings, user);
            } catch (Exception e) {
                trace("❌ Error al crear la sala: " + e.getMessage());
                throw e;
            }

            ISFSObject response = new SFSObject();
            response.putUtfString("codigo", codigo);
            response.putUtfString("type", tipo);
            response.putUtfString("nombre", nombre);
            response.putInt("maxusers", maxUsers);
            response.putUtfString("roomName", roomName);
            send("crear_sala", response, user);

            trace("✅ Sala creada correctamente: " + roomName);

        } catch (Exception e) {
            trace("❌ Error en crear_sala: " + e.getMessage());
            for (StackTraceElement el : e.getStackTrace()) {
                trace("    at " + el.toString());
            }

            ISFSObject response = new SFSObject();
            response.putUtfString("error", "Error interno al crear sala.");
            send("crear_sala", response, user);
        }
    }

    private String generarCodigo() {
        Random rand = new Random();
        int code = 100000 + rand.nextInt(900000);
        return String.valueOf(code);
    }
}
