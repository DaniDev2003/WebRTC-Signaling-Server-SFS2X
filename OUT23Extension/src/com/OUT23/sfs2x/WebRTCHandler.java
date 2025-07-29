package com.OUT23.sfs2x;

import com.smartfoxserver.v2.extensions.BaseClientRequestHandler;
import com.smartfoxserver.v2.entities.*;
import com.smartfoxserver.v2.entities.variables.*;
import com.smartfoxserver.v2.entities.data.*;
import java.util.*;

public class WebRTCHandler extends BaseClientRequestHandler {

    @Override
    public void handleClientRequest(User sender, ISFSObject params) {
        String codigo = params.getUtfString("codigo");
        if (codigo == null) {
            trace("Falta 'codigo' en parámetros.");
            return;
        }

        Room room = getParentExtension().getParentZone().getRoomByName(codigo);
        if (room == null) {
            trace("Sala no encontrada: " + codigo);
            return;
        }

        RoomVariable jugadoresVar = room.getVariable("jugadores");
        ISFSArray jugadoresArray = (jugadoresVar != null) ? (SFSArray) jugadoresVar.getValue() : new SFSArray();

        String ownerId = params.getUtfString("ownerId");
        ISFSObject jugadorData = null;

        // Buscar si ya existe jugador
        for (int i = 0; i < jugadoresArray.size(); i++) {
            ISFSObject obj = jugadoresArray.getSFSObject(i);
            if (obj.getUtfString("ownerId").equals(ownerId)) {
                jugadorData = obj;
                break;
            }
        }

        // Si no existe, crearlo
        if (jugadorData == null) {
            jugadorData = new SFSObject();
            jugadorData.putUtfString("ownerId", ownerId);
            jugadorData.putSFSArray("sdpOffers", new SFSArray());
            jugadorData.putSFSArray("answers", new SFSArray());
            jugadorData.putSFSArray("iceCandidates", new SFSArray());
            jugadoresArray.addSFSObject(jugadorData);
        }

        // Manejar sdpOffer
     // Manejar sdpOffer
        if (params.containsKey("sdpOffer")) {
            ISFSObject newOffer = params.getSFSObject("sdpOffer");

            // Verificar si 'sdpOffers' ya existe en el objeto del jugador
            ISFSArray sdpOffersArray;
            if (jugadorData.containsKey("sdpOffers") && jugadorData.get("sdpOffers") instanceof ISFSArray) {
                sdpOffersArray = jugadorData.getSFSArray("sdpOffers");
            } else {
                // Si no existe, la creamos y la asignamos
                sdpOffersArray = new SFSArray();
                jugadorData.putSFSArray("sdpOffers", sdpOffersArray);
            }

            // Revisar si ya existe una offer para ese 'for'
            String target = newOffer.getUtfString("for");
            boolean exists = false;

            for (int i = 0; i < sdpOffersArray.size(); i++) {
                ISFSObject existing = sdpOffersArray.getSFSObject(i);
                if (existing.getUtfString("for").equals(target)) {
                    exists = true;
                    break;
                }
            }

            // Agregar si no existe
            if (!exists) {
                sdpOffersArray.addSFSObject(newOffer);
            }
        }

        // Manejar answer
        if (params.containsKey("answer")) {
            ISFSObject answer = params.getSFSObject("answer");
            ISFSArray answersArray = jugadorData.getSFSArray("answers");

            String fromId = answer.getUtfString("fromId");
            boolean exists = false;

            for (int i = 0; i < answersArray.size(); i++) {
                ISFSObject existing = answersArray.getSFSObject(i);
                if (existing.getUtfString("fromId").equals(fromId)) {
                    exists = true;
                    break;
                }
            }

            if (!exists) {
                answersArray.addSFSObject(answer);
            }
        }

        // Manejar iceCandidate
        if (params.containsKey("iceCandidate")) {
            ISFSObject candidateData = params.getSFSObject("iceCandidate");
            ISFSArray iceArray = jugadorData.getSFSArray("iceCandidates");

            String target = candidateData.getUtfString("for");
            ISFSObject candidate = candidateData.getSFSObject("candidate");

            // Crear una copia unificada con estructura plana
            ISFSObject mergedCandidate = new SFSObject();
            mergedCandidate.putUtfString("for", target);
            mergedCandidate.putUtfString("candidate", candidate.getUtfString("candidate"));
            mergedCandidate.putUtfString("sdpMid", candidate.getUtfString("sdpMid"));
            mergedCandidate.putInt("sdpMLineIndex", candidate.getInt("sdpMLineIndex"));

            boolean exists = false;
            for (int i = 0; i < iceArray.size(); i++) {
                ISFSObject existing = iceArray.getSFSObject(i);
                if (existing.getUtfString("for").equals(target)
                    && existing.getUtfString("candidate").equals(mergedCandidate.getUtfString("candidate"))) {
                    exists = true;
                    break;
                }
            }

            if (!exists) {
                iceArray.addSFSObject(mergedCandidate);
            }
        }

        // Guardar variable actualizada
        RoomVariable newVar = new SFSRoomVariable("jugadores", jugadoresArray);
        newVar.setGlobal(true);
        newVar.setPersistent(true);

        getApi().setRoomVariables(null, room, Arrays.asList(newVar));

        trace("Datos WebRTC actualizados en sala: " + codigo);
    }
}
