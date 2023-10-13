using System;
using UnityEngine;
using UnityEngine.UI;
using VContainer;


    public class IPJoiningUI : MonoBehaviour
    {
        [SerializeField]
        CanvasGroup m_CanvasGroup;

        [SerializeField] InputField m_IPInputField;

        [SerializeField] InputField m_PortInputField;

        [SerializeField]
        Button m_JoinButton;

        [Inject] IPUIMediator m_IPUIMediator;

        void Awake()
        {
            m_IPInputField.text = IPUIMediator.k_DefaultIP;
            m_PortInputField.text = IPUIMediator.k_DefaultPort.ToString();
        }

        public void Show()
        {
            m_CanvasGroup.alpha = 1f;
            m_CanvasGroup.blocksRaycasts = true;
        }

        public void Hide()
        {
            m_CanvasGroup.alpha = 0f;
            m_CanvasGroup.blocksRaycasts = false;
        }

        public void OnJoinButtonPressed()
        {
            m_IPUIMediator.JoinWithIP(m_IPInputField.text, m_PortInputField.text);
        }

        /// <summary>
        /// Added to the InputField component's OnValueChanged callback for the Room/IP UI text.
        /// </summary>
        public void SanitizeIPInputText()
        {
            m_IPInputField.text = IPUIMediator.SanitizeIP(m_IPInputField.text);
            m_JoinButton.interactable = IPUIMediator.AreIpAddressAndPortValid(m_IPInputField.text, m_PortInputField.text);
        }

        /// <summary>
        /// Added to the InputField component's OnValueChanged callback for the Port UI text.
        /// </summary>
        public void SanitizePortText()
        {
            m_PortInputField.text = IPUIMediator.SanitizePort(m_PortInputField.text);
            m_JoinButton.interactable = IPUIMediator.AreIpAddressAndPortValid(m_IPInputField.text, m_PortInputField.text);
        }
    }

